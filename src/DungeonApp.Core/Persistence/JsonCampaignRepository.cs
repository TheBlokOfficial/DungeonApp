using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
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
        var temporaryPaths = new List<string>();
        string? temporaryManifestPath = null;

        try
        {
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

                temporaryPaths.Add(await WriteDataBlockAsync(directory, id, document, cancellationToken));
                entries.Add(new DataBlockEntry(id.Value, registration.Version, generation));
            }

            // A data block this session could not read - unknown id, or an unsupported stored version
            // - keeps its recorded entry and its file exactly as they were, at the generation they
            // were last written at. That is the whole point of never loading it into memory: nothing
            // here has anything new to write for it.
            entries.AddRange(RetainedEntries(previous, campaign.DataBlocks));

            var manifestDocument = new CampaignManifest(
                CurrentFormatVersion,
                campaign.Id.Value,
                campaign.Name.Value,
                campaign.CreatedAt,
                // Reserved so the first ruleset and content pack do not force a format migration.
                Ruleset: null,
                ContentPacks: [],
                Generation: generation,
                DataBlocks: entries);

            temporaryManifestPath = await WriteTemporaryAsync(manifestPath, manifestDocument, cancellationToken);

            foreach (var temporaryPath in temporaryPaths)
            {
                File.Move(temporaryPath, StripTemporarySuffix(temporaryPath), overwrite: true);
            }

            // The manifest lands last. Its arrival is what marks the whole generation as committed,
            // and what a torn save is measured against.
            File.Move(temporaryManifestPath, manifestPath, overwrite: true);
        }
        finally
        {
            // The manifest's own temporary belongs here too: a move that fails partway through the
            // data block files would otherwise leave it behind for the next save to trip over.
            IEnumerable<string> leftovers = temporaryManifestPath is null
                ? temporaryPaths
                : [.. temporaryPaths, temporaryManifestPath];

            foreach (var temporaryPath in leftovers.Where(File.Exists))
            {
                File.Delete(temporaryPath);
            }
        }
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

        try
        {
            // Built, then hydrated in one call - unlike the old module path, there is no activation
            // step: data blocks are data, not behaviour, so there is nothing to instantiate first.
            return Campaign.Restore(new CampaignId(manifest.Id), name, manifest.CreatedAt, registry, values, unreadable);
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

    private async Task<string> WriteDataBlockAsync(
        string directory,
        DataBlockId id,
        DataBlockDocument document,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(directory, DataBlockDirectoryName));

        return await WriteTemporaryAsync(GetDataBlockPath(directory, id), document, cancellationToken);
    }

    /// <summary>
    /// Serializes beside the destination and returns the temporary path. Nothing is replaced until
    /// every file in the generation has serialized cleanly.
    /// </summary>
    private async Task<string> WriteTemporaryAsync<TDocument>(
        string destinationPath,
        TDocument document,
        CancellationToken cancellationToken)
    {
        var temporaryPath = destinationPath + TemporarySuffix;

        await using var stream = File.Create(temporaryPath);
        await JsonSerializer.SerializeAsync(stream, document, _serializerOptions, cancellationToken);

        return temporaryPath;
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

    private const string TemporarySuffix = ".writing.tmp";

    private static string StripTemporarySuffix(string temporaryPath) =>
        temporaryPath[..^TemporarySuffix.Length];

    private string GetCampaignDirectory(CampaignId id) => Path.Combine(libraryPath, id.Value.ToString("D"));

    private static string GetDataBlockPath(string campaignDirectory, DataBlockId id) =>
        Path.Combine(campaignDirectory, DataBlockDirectoryName, $"{id.Value}.json");

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        string? Ruleset,
        IReadOnlyList<string>? ContentPacks,
        long Generation,
        IReadOnlyList<DataBlockEntry>? DataBlocks);

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
}
