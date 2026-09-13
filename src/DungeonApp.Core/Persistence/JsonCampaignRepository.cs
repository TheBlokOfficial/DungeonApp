using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stores each campaign as its own directory, in the shape of a game save: a manifest beside smaller
/// files owned by whoever the data belongs to.
/// <para>
/// The split follows consistency boundaries rather than subject matter. The manifest and the data
/// block value files commit together and share a generation counter, so a save interrupted between
/// them is detectable instead of silently half loaded.
/// </para>
/// <para>
/// A data block this build cannot read - because its id is not registered, or its stored value is at
/// a shape version this build has no migration for - is carried through untouched: its value file is
/// never rewritten and its manifest entry is copied forward exactly as it was. See
/// <see cref="CampaignDataBlocks.UnreadableBlocks"/> for how the campaign itself represents that.
/// </para>
/// </summary>
public sealed class JsonCampaignRepository(
    string libraryPath,
    DataBlockRegistry registry) : ICampaignRepository
{
    /// <summary>Independent of the application version: it only ever tracks the shape of these files.</summary>
    public const int CurrentFormatVersion = 1;

    private const string DocumentFileName = "campaign.json";
    private const string DataBlockDirectoryName = "datablocks";
    private const string InstanceDirectoryName = "instances";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        var directory = GetCampaignDirectory(campaign.Id);
        Directory.CreateDirectory(directory);

        var manifestPath = Path.Combine(directory, DocumentFileName);

        // Read before write: the counter belongs to the store, not to the campaign, so a storage
        // concept never leaks into the domain model.
        var previous = await TryReadManifestAsync(manifestPath, cancellationToken);
        var generation = (previous?.Generation ?? 0) + 1;

        var entries = new List<DataBlockEntry>();

        using var atomic = new AtomicWrite();

        foreach (var id in campaign.DataBlocks.WrittenBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registration = registry.Describe(id);
            var value = campaign.DataBlocks.Read(id)
                ?? throw new InvalidOperationException($"Data block '{id}' is listed as written but reads back null.");

            var document = new DataBlockDocument(
                BlockId: id.Value,
                Version: registration.Version,
                Generation: generation,
                Value: DataBlockValueSerializer.ToNode(value, registration.Shape));

            await WriteDataBlockAsync(atomic, directory, id, document, cancellationToken);
            entries.Add(new DataBlockEntry(id.Value, registration.Version, generation));
        }

        // A data block this session could not read - unknown id, or an unsupported stored version
        // - keeps its recorded entry and its file exactly as they were, at the generation they
        // were last written at. That is the whole point of never loading it into memory: nothing
        // here has anything new to write for it.
        entries.AddRange(RetainedEntries(previous, campaign.DataBlocks));

        // Unlike data blocks, an instance has no "this session could not read it" carve-out: the
        // whole world lives in memory, so every instance is rewritten every save, at this
        // generation, and the manifest ends up listing exactly what campaign.Instances holds right
        // now - nothing retained from a prior save.
        var instanceEntries = new List<InstanceEntry>();

        foreach (var instance in campaign.Instances.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = new InstanceDocument(
                instance.Id.Value,
                instance.Source.Pack.Value,
                instance.Source.Entry.Value,
                instance.Label,
                instance.Patch.Read<JsonElement>(),
                generation);

            await WriteInstanceAsync(atomic, directory, instance.Id, document, cancellationToken);
            instanceEntries.Add(new InstanceEntry(instance.Id.Value, generation));
        }

        var manifestDocument = new CampaignManifest(
            CurrentFormatVersion,
            campaign.Id.Value,
            campaign.Name.Value,
            campaign.CreatedAt,
            Generation: generation,
            DataBlocks: entries,
            Instances: instanceEntries);

        // Staged last, so Commit moves it last: its arrival is what marks the whole generation as
        // committed, and what a torn save is measured against.
        await atomic.StageAsync(
            manifestPath,
            (stream, token) => JsonSerializer.SerializeAsync(stream, manifestDocument, _serializerOptions, token),
            cancellationToken);

        atomic.Commit();

        // Only reachable once the manifest above has actually landed, and that is what makes this
        // safe: the manifest is the index, so a file this generation's manifest does not list can
        // never be read back by anyone, regardless of when - or whether - it is actually removed
        // from disk. Deleting before Commit would instead risk destroying a file the *previous*,
        // still-current manifest pointed at, if the write failed partway through. An I/O failure
        // here must not undo a save that already committed, so it is swallowed rather than thrown:
        // the worst outcome is a leftover file, picked up by this same cleanup on the next save.
        CleanupRemovedInstanceFiles(directory, instanceEntries);
    }

    public async Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        var directory = GetCampaignDirectory(id);
        var manifestPath = Path.Combine(directory, DocumentFileName);

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = await ReadManifestAsync(manifestPath, cancellationToken);
        var name = ValidateManifest(manifest, manifestPath);

        var values = new Dictionary<DataBlockId, object>();
        var unreadable = new Dictionary<DataBlockId, DataBlockUnreadableReason>();

        foreach (var entry in manifest.DataBlocks ?? [])
        {
            if (!DataBlockId.TryCreate(entry.Id, out var blockId))
            {
                throw new CampaignStoreException(
                    CampaignStoreFailure.Invalid, $"The campaign at {manifestPath} names an unusable data block '{entry.Id}'.");
            }

            if (!registry.Knows(blockId))
            {
                // Not an error: a build older or newer than the one that wrote this save may simply
                // not have this data block. It stays on the shelf, unread, rather than blocking the
                // whole campaign from opening.
                unreadable[blockId] = DataBlockUnreadableReason.UnknownToRegistry;
                continue;
            }

            var registration = registry.Describe(blockId);

            if (entry.Version != registration.Version)
            {
                // No migration path exists yet (deliberately deferred) - a version this build does not
                // recognize is left untouched rather than guessed at.
                unreadable[blockId] = DataBlockUnreadableReason.UnsupportedVersion;
                continue;
            }

            values[blockId] = await ReadDataBlockValueAsync(
                directory, blockId, entry, registration, manifestPath, cancellationToken);
        }

        var instances = new List<CampaignInstance>();

        foreach (var entry in manifest.Instances ?? [])
        {
            instances.Add(await ReadInstanceAsync(directory, entry, cancellationToken));
        }

        try
        {
            // Built, then hydrated in one call - unlike the old module path, there is no activation
            // step: data blocks are data, not behaviour, so there is nothing to instantiate first.
            return Campaign.Restore(
                new CampaignId(manifest.Id), name, manifest.CreatedAt, registry, values, unreadable, instances);
        }
        catch (DataBlockShapeMismatchException ex)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid,
                $"The campaign at {manifestPath} has a data block value that does not fit its declared shape.",
                ex);
        }
    }

    /// <summary>
    /// Reads manifests only. Drawing the shelf must not cost the value of every data block in every
    /// campaign, and a campaign whose data block values are torn still deserves to be listed.
    /// </summary>
    public async Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(libraryPath))
        {
            return [];
        }

        var summaries = new List<CampaignSummary>();

        foreach (var directory in Directory.EnumerateDirectories(libraryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var manifestPath = Path.Combine(directory, DocumentFileName);

            if (!File.Exists(manifestPath))
            {
                continue;
            }

            try
            {
                var manifest = await ReadManifestAsync(manifestPath, cancellationToken);
                var name = ValidateManifest(manifest, manifestPath);

                summaries.Add(new CampaignSummary(new CampaignId(manifest.Id), name, manifest.CreatedAt));
            }
            catch (CampaignStoreException)
            {
                // One damaged campaign must not hide the whole shelf. Telling the GM about it is a
                // separate job for the library screen, not a reason to fail the entire read.
            }
        }

        return summaries
            .OrderBy(summary => summary.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(summary => summary.CreatedAt)
            .ToArray();
    }

    public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = GetCampaignDirectory(id);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private CampaignName ValidateManifest(CampaignManifest manifest, string path)
    {
        // A newer format is refused whole. Guessing at fields a future build added is exactly how a
        // partial read silently drops half a campaign.
        if (manifest.FormatVersion > CurrentFormatVersion)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.UnsupportedFormatVersion,
                $"The campaign at {path} uses format version {manifest.FormatVersion}; "
                + $"this build understands up to {CurrentFormatVersion}.");
        }

        if (manifest.FormatVersion < 1 || manifest.Id == Guid.Empty)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"The campaign document at {path} has no usable identity.");
        }

        if (!CampaignName.TryCreate(manifest.Name, out var name))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"The campaign document at {path} has no usable name.");
        }

        return name;
    }

    private async Task<object> ReadDataBlockValueAsync(
        string directory,
        DataBlockId id,
        DataBlockEntry entry,
        DataBlockRegistration registration,
        string manifestPath,
        CancellationToken cancellationToken)
    {
        var valuePath = GetDataBlockPath(directory, id);

        if (!File.Exists(valuePath))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave, $"The value file for data block '{id}' is missing.");
        }

        DataBlockDocument? document;

        try
        {
            await using var stream = File.OpenRead(valuePath);
            document = await JsonSerializer.DeserializeAsync<DataBlockDocument>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the value of data block '{id}'.", ex);
        }

        if (document is null)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The value file for data block '{id}' is empty.");
        }

        // The whole point of the counter: a file left behind by an interrupted save disagrees with
        // the manifest, and says so instead of loading as if it were current.
        if (document.Generation != entry.Generation)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave,
                $"Data block '{id}' is stored at generation {document.Generation} but the manifest "
                + $"expects {entry.Generation}. The save was interrupted.");
        }

        try
        {
            return DataBlockValueSerializer.FromNode(document.Value, registration.Shape);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or JsonException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"The stored value of data block '{id}' does not fit its shape.", ex);
        }
    }

    private static IEnumerable<DataBlockEntry> RetainedEntries(CampaignManifest? previous, CampaignDataBlocks dataBlocks)
    {
        var written = new HashSet<DataBlockId>(dataBlocks.WrittenBlocks);

        return (previous?.DataBlocks ?? [])
            .Where(entry => !DataBlockId.TryCreate(entry.Id, out var id) || !written.Contains(id));
    }

    private async Task WriteDataBlockAsync(
        AtomicWrite atomic,
        string directory,
        DataBlockId id,
        DataBlockDocument document,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(directory, DataBlockDirectoryName));

        await atomic.StageAsync(
            GetDataBlockPath(directory, id),
            (stream, token) => JsonSerializer.SerializeAsync(stream, document, _serializerOptions, token),
            cancellationToken);
    }

    private async Task WriteInstanceAsync(
        AtomicWrite atomic,
        string directory,
        InstanceId id,
        InstanceDocument document,
        CancellationToken cancellationToken)
    {
        // Created here, inside the per-instance loop, for the same reason WriteDataBlockAsync
        // creates its own sibling directory: a campaign with no instance ever calls this method, so
        // the directory never comes into being for one - there is nothing to leave empty.
        Directory.CreateDirectory(Path.Combine(directory, InstanceDirectoryName));

        await atomic.StageAsync(
            GetInstancePath(directory, id.Value),
            (stream, token) => JsonSerializer.SerializeAsync(stream, document, _serializerOptions, token),
            cancellationToken);
    }

    private async Task<CampaignInstance> ReadInstanceAsync(
        string directory, InstanceEntry entry, CancellationToken cancellationToken)
    {
        var instancePath = GetInstancePath(directory, entry.Id);

        if (!File.Exists(instancePath))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave, $"The value file for instance '{entry.Id:D}' is missing.");
        }

        InstanceDocument? document;

        try
        {
            await using var stream = File.OpenRead(instancePath);
            document = await JsonSerializer.DeserializeAsync<InstanceDocument>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the value of instance '{entry.Id:D}'.", ex);
        }

        if (document is null)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The value file for instance '{entry.Id:D}' is empty.");
        }

        // Same reasoning as the data block's generation check: a file left behind by an interrupted
        // save disagrees with the manifest, and says so instead of loading as if it were current.
        if (document.Generation != entry.Generation)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave,
                $"Instance '{entry.Id:D}' is stored at generation {document.Generation} but the manifest "
                + $"expects {entry.Generation}. The save was interrupted.");
        }

        if (!ContentId.TryCreate(document.PackId, out var packId) || !ContentId.TryCreate(document.EntryId, out var entryId))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid,
                $"The instance file at {instancePath} names an unusable entry address "
                + $"'{document.PackId}:{document.EntryId}'.");
        }

        // Checked here rather than left to the envelope, because the envelope is built one line
        // below and would fail inside the serializer instead: an absent patch key deserializes to an
        // undefined element, which throws on the way back out with a message naming neither the
        // campaign nor the instance. Everything wrong with a campaign's own files has to reach the
        // caller as a CampaignStoreException, since that is the only failure the shell knows how to
        // degrade on.
        if (document.Patch.ValueKind == JsonValueKind.Undefined)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The value file for instance '{entry.Id:D}' carries no patch.");
        }

        if (document.Patch.ValueKind != JsonValueKind.Object)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid,
                $"The patch stored for instance '{entry.Id:D}' is {document.Patch.ValueKind}, not a JSON object.");
        }

        return new CampaignInstance(
            new InstanceId(document.InstanceId),
            new EntryAddress(packId, entryId),
            document.Label,
            ContentValues.From(document.Patch));
    }

    /// <summary>
    /// Removes any <c>.json</c> file left in the instances directory that the manifest just staged
    /// does not list - the file for an instance the GM removed this save. Only ever called after
    /// <see cref="AtomicWrite.Commit"/> has already succeeded; see the caller for why that order is
    /// the whole point.
    /// </summary>
    private static void CleanupRemovedInstanceFiles(string campaignDirectory, IReadOnlyList<InstanceEntry> currentEntries)
    {
        var instancesDirectory = Path.Combine(campaignDirectory, InstanceDirectoryName);

        if (!Directory.Exists(instancesDirectory))
        {
            return;
        }

        var kept = new HashSet<Guid>(currentEntries.Select(e => e.Id));

        foreach (var file in Directory.EnumerateFiles(instancesDirectory, "*.json"))
        {
            if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(file), "D", out var id) && kept.Contains(id))
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A save that already committed must not fail over housekeeping. The file is picked
                // up again by this same cleanup on the next save.
            }
        }
    }

    private async Task<CampaignManifest?> TryReadManifestAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await ReadManifestAsync(path, cancellationToken);
        }
        catch (CampaignStoreException)
        {
            // An unreadable manifest is about to be replaced. It cannot be trusted to say which
            // generation the data block files are at, so the counter restarts and every written block
            // is rewritten at the new one; anything retained keeps whatever the unreadable manifest
            // would have said, which is nothing.
            return null;
        }
    }

    private async Task<CampaignManifest> ReadManifestAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync<CampaignManifest>(
                stream, _serializerOptions, cancellationToken);

            return manifest ?? throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The campaign document at {path} is empty.");
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the campaign document at {path}.", ex);
        }
    }

    private string GetCampaignDirectory(CampaignId id) => Path.Combine(libraryPath, id.Value.ToString("D"));

    private static string GetDataBlockPath(string campaignDirectory, DataBlockId id) =>
        Path.Combine(campaignDirectory, DataBlockDirectoryName, $"{id.Value}.json");

    private static string GetInstancePath(string campaignDirectory, Guid instanceId) =>
        Path.Combine(campaignDirectory, InstanceDirectoryName, $"{instanceId:D}.json");

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        long Generation,
        IReadOnlyList<DataBlockEntry>? DataBlocks,
        // Nullable and defaulted nowhere near strictly, on purpose: a manifest written before
        // instances existed simply has no such key, and that has to keep opening as a campaign
        // with an empty world rather than refusing the file or forcing a format bump.
        IReadOnlyList<InstanceEntry>? Instances = null);

    /// <summary>
    /// What the manifest remembers about one data block's value file: which shape version it is in,
    /// and which generation it was last written at. The second half is what makes a torn save
    /// detectable.
    /// </summary>
    private sealed record DataBlockEntry(string Id, int Version, long Generation);

    /// <summary>
    /// The envelope around one data block's value. The value itself stays an opaque JSON node until
    /// the block's registered shape is known, so the store never guesses at a type before checking.
    /// </summary>
    private sealed record DataBlockDocument(string BlockId, int Version, long Generation, JsonNode? Value);

    /// <summary>
    /// What the manifest remembers about one instance's value file. Mirrors
    /// <see cref="DataBlockEntry"/> exactly, minus the shape version an instance has no equivalent
    /// of: the entry it points at is resolved from the pack registry, not from anything this store
    /// tracks a version number for.
    /// </summary>
    private sealed record InstanceEntry(Guid Id, long Generation);

    /// <summary>
    /// The envelope around one instance's stored state. <see cref="PackId"/> and <see cref="EntryId"/>
    /// are the two halves of its <see cref="EntryAddress"/>, carried as plain strings rather than as
    /// <see cref="ContentId"/> itself so that a value this build cannot reparse into one is still
    /// readable as a document - the failure is reported once, deliberately, in
    /// <see cref="ReadInstanceAsync"/>, rather than by the deserializer throwing before this type
    /// even has a chance to say which instance is at fault.
    /// <para>
    /// <see cref="Patch"/> stays an opaque <see cref="JsonElement"/>, the same way
    /// <see cref="DataBlockDocument.Value"/> stays an opaque <see cref="JsonNode"/>: this store never
    /// opens a patch, it only carries the sealed <see cref="ContentValues"/> envelope through
    /// <see cref="ContentValues.Read{T}"/> and <see cref="ContentValues.From{T}"/> unchanged.
    /// </para>
    /// </summary>
    private sealed record InstanceDocument(
        Guid InstanceId,
        string PackId,
        string EntryId,
        string? Label,
        JsonElement Patch,
        long Generation);
}
