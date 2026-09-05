using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stores each campaign as its own directory, in the shape of a game save: a manifest beside
/// smaller files owned by whoever the state belongs to.
/// <para>
/// The split follows consistency boundaries rather than subject matter. The manifest and the module
/// states commit together and share a generation counter, so a save interrupted between them is
/// detectable instead of silently half loaded.
/// </para>
/// <para>
/// State belonging to a module that is switched off, or that this build has never heard of, is
/// carried through untouched. Disabling a module and enabling it again must not cost the GM its
/// contents.
/// </para>
/// </summary>
public sealed class JsonCampaignRepository(
    string libraryPath,
    ModuleCatalog catalog) : ICampaignRepository
{
    /// <summary>Independent of the application version: it only ever tracks the shape of these files.</summary>
    public const int CurrentFormatVersion = 1;

    private const string DocumentFileName = "campaign.json";
    private const string ModuleDirectoryName = "modules";

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

        var entries = new List<ModuleEntry>();
        var temporaryPaths = new List<string>();
        string? temporaryManifestPath = null;

        try
        {
            foreach (var module in campaign.Modules.Active)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var manifest = module.Manifest;
                var state = new ModuleStateDocument(
                    manifest.Id.Value,
                    manifest.StateVersion,
                    generation,
                    JsonSerializer.SerializeToElement(module.CaptureState(), module.StateType, _serializerOptions));

                temporaryPaths.Add(await WriteModuleStateAsync(directory, manifest.Id, state, cancellationToken));
                entries.Add(new ModuleEntry(manifest.Id.Value, manifest.StateVersion, generation));
            }

            // Everything the campaign is not currently running keeps its recorded entry and its file
            // exactly as they were, at the generation they were last written at.
            entries.AddRange(RetainedEntries(previous, campaign.Modules));

            var document = new CampaignManifest(
                CurrentFormatVersion,
                campaign.Id.Value,
                campaign.Name.Value,
                campaign.CreatedAt,
                // Reserved so the first ruleset and content pack do not force a format migration.
                Ruleset: null,
                ContentPacks: [],
                Generation: generation,
                ActiveModules: [.. campaign.Modules.Active.Select(module => module.Manifest.Id.Value)],
                Modules: entries);

            temporaryManifestPath = await WriteTemporaryAsync(manifestPath, document, cancellationToken);

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
            // module files would otherwise leave it behind for the next save to trip over.
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

        // Built, then wired by Campaign.Restore, then filled. Activation happens exactly once,
        // inside the campaign, before any module state is restored onto it.
        var campaign = Campaign.Restore(
            new CampaignId(manifest.Id),
            name,
            manifest.CreatedAt,
            CreateModules(manifest, manifestPath));

        foreach (var module in campaign.Modules.Active)
        {
            await RestoreModuleStateAsync(directory, module, manifest, manifestPath, cancellationToken);
        }

        return campaign;
    }

    /// <summary>
    /// Reads manifests only. Drawing the shelf must not cost the state of every module in every
    /// campaign, and a campaign whose module state is torn still deserves to be listed.
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

    /// <summary>
    /// Turns the manifest's list of switched-on modules into instances, or refuses. No activation
    /// and no state here - only the question of whether this build can make what the save names.
    /// </summary>
    private IReadOnlyList<ICampaignModule> CreateModules(CampaignManifest manifest, string manifestPath)
    {
        var active = new List<ICampaignModule>();

        foreach (var rawId in manifest.ActiveModules ?? [])
        {
            if (!ModuleId.TryCreate(rawId, out var id))
            {
                throw new CampaignStoreException(
                    CampaignStoreFailure.Invalid, $"The campaign at {manifestPath} names an unusable module '{rawId}'.");
            }

            // Refused rather than skipped: opening a campaign without a module it is supposed to be
            // running would quietly show the GM an incomplete world. The state file is untouched,
            // so a build that has the module can still open it.
            if (!catalog.Knows(id))
            {
                throw new CampaignStoreException(
                    CampaignStoreFailure.UnknownModule,
                    $"The campaign at {manifestPath} needs module '{id}', which is not part of this build.");
            }

            active.Add(catalog.Create(id));
        }

        return active;
    }

    private async Task RestoreModuleStateAsync(
        string directory,
        ICampaignModule module,
        CampaignManifest manifest,
        string manifestPath,
        CancellationToken cancellationToken)
    {
        var id = module.Manifest.Id;

        var entry = (manifest.Modules ?? []).FirstOrDefault(candidate => candidate.Id == id.Value)
            ?? throw new CampaignStoreException(
                CampaignStoreFailure.TornSave,
                $"The campaign at {manifestPath} runs module '{id}' but records no state for it.");

        var statePath = GetModuleStatePath(directory, id);

        if (!File.Exists(statePath))
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave, $"The state file for module '{id}' is missing.");
        }

        ModuleStateDocument? document;

        try
        {
            await using var stream = File.OpenRead(statePath);
            document = await JsonSerializer.DeserializeAsync<ModuleStateDocument>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the state of module '{id}'.", ex);
        }

        if (document is null)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The state file for module '{id}' is empty.");
        }

        // The whole point of the counter: a file left behind by an interrupted save disagrees with
        // the manifest, and says so instead of loading as if it were current.
        if (document.Generation != entry.Generation)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.TornSave,
                $"Module '{id}' is stored at generation {document.Generation} but the manifest "
                + $"expects {entry.Generation}. The save was interrupted.");
        }

        object? state;

        try
        {
            state = document.State.Deserialize(module.StateType, _serializerOptions);
        }
        catch (JsonException ex)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"The stored state of module '{id}' does not fit its type.", ex);
        }

        if (state is null)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Invalid, $"The stored state of module '{id}' is null.");
        }

        // The recorded version, not the current one: migrating an older shape is the module's job.
        module.RestoreState(state, document.StateVersion);
    }

    private static IEnumerable<ModuleEntry> RetainedEntries(CampaignManifest? previous, CampaignModules active) =>
        (previous?.Modules ?? [])
            .Where(entry => !ModuleId.TryCreate(entry.Id, out var id) || !active.Contains(id));

    private async Task<string> WriteModuleStateAsync(
        string directory,
        ModuleId id,
        ModuleStateDocument state,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.Combine(directory, ModuleDirectoryName));

        return await WriteTemporaryAsync(GetModuleStatePath(directory, id), state, cancellationToken);
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
            // generation the module files are at, so the counter restarts and every active module is
            // rewritten at the new one; anything retained keeps whatever the unreadable manifest
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

    private static string GetModuleStatePath(string campaignDirectory, ModuleId id) =>
        Path.Combine(campaignDirectory, ModuleDirectoryName, $"{id.Value}.json");

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        string? Ruleset,
        IReadOnlyList<string>? ContentPacks,
        long Generation,
        IReadOnlyList<string>? ActiveModules,
        IReadOnlyList<ModuleEntry>? Modules);

    /// <summary>
    /// What the manifest remembers about one module's state file: which shape it is in, and which
    /// generation it was last written at. The second half is what makes a torn save detectable.
    /// </summary>
    private sealed record ModuleEntry(string Id, int StateVersion, long Generation);

    /// <summary>
    /// The envelope around a module's own state. The state itself stays an opaque element until the
    /// module's declared type is known, so the store never needs to understand what it holds.
    /// </summary>
    private sealed record ModuleStateDocument(
        string ModuleId,
        int StateVersion,
        long Generation,
        JsonElement State);
}
