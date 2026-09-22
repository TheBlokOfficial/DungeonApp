using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stores each campaign as its own directory: a manifest beside one file per state model the
/// system declares (<c>state/&lt;modelId&gt;.json</c>) - never one file per record any more, now
/// that a model's whole record set is what gets versioned and read back together.
/// <para>
/// The whole of every declared model is rewritten on every save (docs/architecture.md, "Gdzie
/// mieszka stan": "Każda zatwierdzona zmiana trafia na dysk od razu"), deliberately without a
/// "changed models only" optimization - the set of files this writes is exactly
/// <paramref name="declarations"/>'s ids, every time. The manifest and every model file commit
/// together and share a generation counter, so a save interrupted partway through is detectable
/// instead of silently half loaded - the same guarantee the old per-instance files carried, now
/// applied per model.
/// </para>
/// <para>
/// A model this build's <paramref name="declarations"/> does not declare is never opened, written,
/// or deleted, however old its file on disk - "Plik modelu, którego nikt nie zadeklarował →
/// nieczytany, nietknięty na dysku." A declared model with no file simply reads back empty.
/// </para>
/// </summary>
public sealed class JsonCampaignRepository(string libraryPath) : ICampaignRepository
{
    /// <summary>
    /// Bumped from the single-file-per-instance shape this format used before state models existed.
    /// A manifest at any other version is refused whole rather than migrated or half-read -
    /// docs/architecture.md, "Wersjonowanie".
    /// </summary>
    public const int CurrentFormatVersion = 2;

    private const string ManifestFileName = "campaign.json";
    private const string StateDirectoryName = "state";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task SaveAsync(
        Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(declarations);

        var directory = GetCampaignDirectory(campaign.Id);
        Directory.CreateDirectory(directory);

        var manifestPath = Path.Combine(directory, ManifestFileName);

        // Read before write: the counter belongs to the store, not to the campaign, so a storage
        // concept never leaks into the domain model.
        var previous = await TryReadManifestAsync(manifestPath, cancellationToken);
        var generation = (previous?.Generation ?? 0) + 1;

        using var atomic = new AtomicWrite();

        var modelEntries = new List<ModelEntry>();

        foreach (var declaration in declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var records = campaign.Snapshot.ModelsById.TryGetValue(declaration.ModelId, out var bucket)
                ? bucket.Values
                : [];

            await WriteModelAsync(atomic, directory, declaration, records, generation, cancellationToken);
            modelEntries.Add(new ModelEntry(declaration.ModelId, generation));
        }

        var manifestDocument = new CampaignManifest(
            CurrentFormatVersion,
            campaign.Id.Value,
            campaign.Name.Value,
            campaign.CreatedAt,
            Generation: generation,
            Models: modelEntries);

        // Staged last, so Commit moves it last: its arrival is what marks the whole generation as
        // committed, and what a torn save is measured against.
        await atomic.StageAsync(
            manifestPath,
            (stream, token) => JsonSerializer.SerializeAsync(stream, manifestDocument, _serializerOptions, token),
            cancellationToken);

        atomic.Commit();
    }

    public async Task<Campaign?> GetAsync(
        CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        var directory = GetCampaignDirectory(id);
        var manifestPath = Path.Combine(directory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = await ReadManifestAsync(manifestPath, cancellationToken);
        var name = ValidateManifest(manifest, manifestPath);

        var manifestModels = (manifest.Models ?? []).ToDictionary(entry => entry.Id);
        var modelsById = new Dictionary<string, IReadOnlyDictionary<string, IStateRecord>>();

        foreach (var declaration in declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!manifestModels.TryGetValue(declaration.ModelId, out var modelEntry))
            {
                // Zadeklarowany model bez pliku → model pusty.
                continue;
            }

            modelsById[declaration.ModelId] = await ReadModelAsync(directory, declaration, modelEntry, cancellationToken);
        }

        var snapshot = CampaignStateSnapshot.FromModels(modelsById);
        return Campaign.Restore(new CampaignId(manifest.Id), name, manifest.CreatedAt, snapshot);
    }

    /// <summary>
    /// Reads manifests only. Drawing the shelf must not cost the value of every model in every
    /// campaign, and a campaign whose model files are torn still deserves to be listed.
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

            var manifestPath = Path.Combine(directory, ManifestFileName);

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

        // An older format is refused whole too, distinguishably - docs/architecture.md,
        // "Wersjonowanie": no migration exists, and none is silently attempted.
        if (manifest.FormatVersion < CurrentFormatVersion)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.LegacyFormatVersion,
                $"The campaign at {path} uses format version {manifest.FormatVersion}; "
                + $"this build only opens version {CurrentFormatVersion}.");
        }

        if (manifest.Id == Guid.Empty)
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

    private async Task WriteModelAsync(
        AtomicWrite atomic,
        string directory,
        StateModelDeclaration declaration,
        IEnumerable<IStateRecord> records,
        long generation,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(directory, StateDirectoryName));

        var elements = records
            .Select(record => JsonSerializer.SerializeToElement(record, declaration.RecordType, _serializerOptions))
            .ToArray();

        var document = new StateModelDocument(declaration.Version, generation, elements);

        await atomic.StageAsync(
            GetModelPath(directory, declaration.ModelId),
            (stream, token) => JsonSerializer.SerializeAsync(stream, document, _serializerOptions, token),
            cancellationToken);
    }

    /// <summary>
    /// Reads one model's records back. Every defect is a store failure, sorted into the kinds the
    /// shell knows how to degrade on: <see cref="CampaignStoreFailure.TornSave"/> when the file and
    /// the manifest disagree about which generation this is (or the file is simply missing),
    /// <see cref="CampaignStoreFailure.ModelVersionMismatch"/> when the file was written at a
    /// version this build's declaration does not declare, <see cref="CampaignStoreFailure.Unreadable"/>
    /// when no bytes or no JSON come back at all, and <see cref="CampaignStoreFailure.Invalid"/> when
    /// a record parsed as JSON but not as this model's record type.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, IStateRecord>> ReadModelAsync(
        string directory, StateModelDeclaration declaration, ModelEntry entry, CancellationToken cancellationToken)
    {
        var path = GetModelPath(directory, declaration.ModelId);

        if (!File.Exists(path))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave, $"The state file for model '{declaration.ModelId}' is missing.");
        }

        StateModelDocument? document;

        try
        {
            await using var stream = File.OpenRead(path);
            document = await JsonSerializer.DeserializeAsync<StateModelDocument>(stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the state of model '{declaration.ModelId}'.", ex);
        }

        if (document is null)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The state file for model '{declaration.ModelId}' is empty.");
        }

        if (document.Generation != entry.Generation)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave,
                $"Model '{declaration.ModelId}' is stored at generation {document.Generation} but the manifest "
                + $"expects {entry.Generation}. The save was interrupted.");
        }

        if (document.Version != declaration.Version)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.ModelVersionMismatch,
                $"Model '{declaration.ModelId}' is stored at version {document.Version}; "
                + $"this build declares version {declaration.Version}.");
        }

        var records = new Dictionary<string, IStateRecord>();

        try
        {
            foreach (var element in document.Records)
            {
                var record = (IStateRecord)JsonSerializer.Deserialize(element.GetRawText(), declaration.RecordType, _serializerOptions)!;
                records[record.Id] = record;
            }
        }
        // ArgumentException alongside JsonException: a value type nested two or more levels deep in
        // a record enforces its own invariant in its own constructor, and System.Text.Json does not
        // reliably re-wrap an exception thrown while building a nested constructor argument as a
        // JsonException the way it does one thrown while setting a top-level property. The
        // validation itself still happens nowhere but that constructor; this only translates
        // whatever it throws into the one failure shape the shell knows how to degrade on.
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"A record for model '{declaration.ModelId}' is invalid.", ex);
        }

        return records;
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
            // generation the model files are at, so the counter restarts and every model is
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

    private static string GetModelPath(string campaignDirectory, string modelId) =>
        Path.Combine(campaignDirectory, StateDirectoryName, $"{modelId}.json");

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        long Generation,
        // Nullable and defaulted nowhere near strictly, on purpose: a manifest for a campaign that
        // has never had a single record in any model simply has no such key.
        IReadOnlyList<ModelEntry>? Models = null);

    /// <summary>What the manifest remembers about one model's state file: its id and which
    /// generation it was last written at. The second half is what makes a torn save detectable.
    /// </summary>
    private sealed record ModelEntry(string Id, long Generation);

    /// <summary>
    /// One model's whole state file: the version it was written at, the save generation it belongs
    /// to, and every record, each still an opaque <see cref="JsonElement"/> here - this store never
    /// opens a record any further than that; only <see cref="StateModelDeclaration.RecordType"/>
    /// (asked of by the caller, never named here) says what shape one actually has.
    /// </summary>
    private sealed record StateModelDocument(int Version, long Generation, IReadOnlyList<JsonElement> Records);
}
