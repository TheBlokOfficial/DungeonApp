using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stores each campaign as its own directory, in the shape of a game save: a manifest beside smaller
/// files owned by whoever the data belongs to.
/// <para>
/// The split follows consistency boundaries rather than subject matter. The manifest and the instance
/// files commit together and share a generation counter, so a save interrupted between them is
/// detectable instead of silently half loaded.
/// </para>
/// <para>
/// A campaign directory from a build that still wrote a <c>datablocks/</c> subdirectory keeps that
/// subdirectory on disk exactly as it was: this build neither writes nor reads it, the manifest no
/// longer names it, and nothing here cleans it up. Deleting data this build no longer understands is
/// worse than leaving an inert folder behind.
/// </para>
/// </summary>
public sealed class JsonCampaignRepository(string libraryPath) : ICampaignRepository
{
    /// <summary>Independent of the application version: it only ever tracks the shape of these files.</summary>
    public const int CurrentFormatVersion = 1;

    private const string DocumentFileName = "campaign.json";
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

        using var atomic = new AtomicWrite();

        // Every instance is rewritten every save, at this generation, and the manifest ends up
        // listing exactly what campaign.Instances holds right now - nothing retained from a prior
        // save.
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

        var instances = new List<CampaignInstance>();

        foreach (var entry in manifest.Instances ?? [])
        {
            instances.Add(await ReadInstanceAsync(directory, entry, cancellationToken));
        }

        return Campaign.Restore(new CampaignId(manifest.Id), name, manifest.CreatedAt, instances);
    }

    /// <summary>
    /// Reads manifests only. Drawing the shelf must not cost the value of every instance in every
    /// campaign, and a campaign whose instance files are torn still deserves to be listed.
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

    private async Task WriteInstanceAsync(
        AtomicWrite atomic,
        string directory,
        InstanceId id,
        InstanceDocument document,
        CancellationToken cancellationToken)
    {
        // Created here, inside the per-instance loop: a campaign with no instance ever calls this
        // method, so the directory never comes into being for one - there is nothing to leave empty.
        Directory.CreateDirectory(Path.Combine(directory, InstanceDirectoryName));

        await atomic.StageAsync(
            GetInstancePath(directory, id.Value),
            (stream, token) => JsonSerializer.SerializeAsync(stream, document, _serializerOptions, token),
            cancellationToken);
    }

    /// <summary>
    /// Reads one instance back. An instance has no shelf to leave something on, unread, until a build
    /// that understands it comes along - so nothing here may be carried through unopened. Every
    /// defect is a store failure instead, sorted into the three kinds the shell knows how to degrade
    /// on: <see cref="CampaignStoreFailure.TornSave"/> when the file and the manifest disagree about
    /// which generation this is, <see cref="CampaignStoreFailure.Unreadable"/> when no bytes or no
    /// JSON come back at all, and <see cref="CampaignStoreFailure.Invalid"/> when the file parsed but
    /// says something an instance cannot mean.
    /// </summary>
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

        // A file left behind by an interrupted save disagrees with the manifest, and says so instead
        // of loading as if it were current.
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
            // generation the instance files are at, so the counter restarts and every instance is
            // rewritten at the new one.
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

    private static string GetInstancePath(string campaignDirectory, Guid instanceId) =>
        Path.Combine(campaignDirectory, InstanceDirectoryName, $"{instanceId:D}.json");

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        long Generation,
        // Nullable and defaulted nowhere near strictly, on purpose: a manifest written before
        // instances existed simply has no such key, and that has to keep opening as a campaign
        // with an empty world rather than refusing the file or forcing a format bump.
        IReadOnlyList<InstanceEntry>? Instances = null);

    /// <summary>What the manifest remembers about one instance's value file: its id and which
    /// generation it was last written at. The second half is what makes a torn save detectable.
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
    /// <see cref="Patch"/> stays an opaque <see cref="JsonElement"/>: this store never opens a patch,
    /// it only carries the sealed <see cref="ContentValues"/> envelope through
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
