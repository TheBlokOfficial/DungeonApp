using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;

namespace DungeonApp.Core.Persistence;

/// <summary>
/// Stores each campaign as its own directory, in the shape of a game save: a manifest beside
/// smaller files owned by whoever the state belongs to.
/// <para>
/// The split follows consistency boundaries rather than subject matter. The manifest and the module
/// states commit together and share a generation counter; the journal and the desk layout have
/// their own lifecycles and their own criticality, so they live apart and cannot take the campaign
/// down with them.
/// </para>
/// <para>
/// A campaign is meant to be a visible, portable document, so the layout is
/// <c>library/campaign-id/campaign.json</c> with room beside it for modules, backups and, later,
/// assets. The directory is keyed by id and never by name, so that a rename stays a rename.
/// </para>
/// </summary>
public sealed class JsonCampaignRepository(string libraryPath) : ICampaignRepository
{
    /// <summary>Independent of the application version: it only ever tracks the shape of this file.</summary>
    public const int CurrentFormatVersion = 1;

    private const string DocumentFileName = "campaign.json";
    private const string BackupDirectoryName = "backups";
    private const string BackupGenerationPrefix = "gen-";
    private const int MaxBackups = 5;

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

        var destinationPath = Path.Combine(directory, DocumentFileName);

        // Read before write: the counter belongs to the store, not to the campaign, so a storage
        // concept never leaks into the domain model.
        var previousGeneration = await ReadGenerationAsync(destinationPath, cancellationToken);

        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        var manifest = new CampaignManifest(
            CurrentFormatVersion,
            campaign.Id.Value,
            campaign.Name.Value,
            campaign.CreatedAt,
            // Reserved now so the first ruleset and content pack do not force a format migration.
            Ruleset: null,
            ContentPacks: [],
            Generation: previousGeneration + 1,
            // Empty until modules exist. The shape is fixed here so that adding the first one is an
            // entry in this list, not a reshaping of the manifest.
            Modules: []);

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, manifest, _serializerOptions, cancellationToken);
            }

            // Only once the new document exists and serialized cleanly is the old one touched. A
            // failure above must never cost the GM the last good version.
            BackUpExisting(destinationPath, previousGeneration);

            // Written last on purpose: once modules have their own files, the manifest landing is
            // what marks the whole generation as committed.
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public async Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(GetCampaignDirectory(id), DocumentFileName);

        return File.Exists(path) ? await ReadAsync(path, cancellationToken) : null;
    }

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

            var path = Path.Combine(directory, DocumentFileName);

            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var campaign = await ReadAsync(path, cancellationToken);
                summaries.Add(new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt));
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

    private async Task<Campaign> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var manifest = await ReadManifestAsync(path, cancellationToken)
            ?? throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"The campaign document at {path} is empty.");

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

        return Campaign.Restore(new CampaignId(manifest.Id), name, manifest.CreatedAt);
    }

    private async Task<CampaignManifest?> ReadManifestAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<CampaignManifest>(
                stream, _serializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new CampaignStoreException(
                CampaignStoreFailure.Unreadable, $"Could not read the campaign document at {path}.", ex);
        }
    }

    /// <summary>
    /// The generation the stored campaign is currently at, or zero when there is nothing readable
    /// there yet.
    /// <para>
    /// Treating an unreadable manifest as generation zero is safe only while modules have no state
    /// files of their own. Once they do, a manifest that cannot be read must not be allowed to
    /// restart the counter underneath them.
    /// </para>
    /// </summary>
    private async Task<long> ReadGenerationAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            var manifest = await ReadManifestAsync(path, cancellationToken);
            return manifest?.Generation ?? 0;
        }
        catch (CampaignStoreException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Preserves the generation about to be replaced, as a set rather than as loose files: once
    /// modules have their own state files, restoring half a generation would be worse than not
    /// restoring at all.
    /// </summary>
    private static void BackUpExisting(string destinationPath, long generation)
    {
        if (!File.Exists(destinationPath))
        {
            return;
        }

        var campaignDirectory = Path.GetDirectoryName(destinationPath)!;
        var backupRoot = Path.Combine(campaignDirectory, BackupDirectoryName);

        // Zero padded so the plain name sort is also the generation order.
        var backupDirectory = Path.Combine(
            backupRoot,
            BackupGenerationPrefix + generation.ToString("D6", CultureInfo.InvariantCulture));

        try
        {
            Directory.CreateDirectory(backupDirectory);
            File.Copy(destinationPath, Path.Combine(backupDirectory, DocumentFileName), overwrite: true);

            var stale = Directory
                .EnumerateDirectories(backupRoot, BackupGenerationPrefix + "*")
                .OrderByDescending(directory => directory, StringComparer.Ordinal)
                .Skip(MaxBackups);

            foreach (var directory in stale)
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A backup is a convenience. Failing to make one must not block saving the campaign.
        }
    }

    private string GetCampaignDirectory(CampaignId id) => Path.Combine(libraryPath, id.Value.ToString("D"));

    private sealed record CampaignManifest(
        int FormatVersion,
        Guid Id,
        string? Name,
        DateTimeOffset CreatedAt,
        string? Ruleset,
        IReadOnlyList<string>? ContentPacks,
        long Generation,
        IReadOnlyList<ModuleEntry>? Modules);

    /// <summary>
    /// What the manifest remembers about one module's state file: which build's shape it is in, and
    /// which generation it was last written at. The second half is what makes a torn save
    /// detectable instead of silent.
    /// </summary>
    private sealed record ModuleEntry(string Id, int StateVersion, long Generation);
}
