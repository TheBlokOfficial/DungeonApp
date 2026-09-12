using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Core.Content;

/// <summary>
/// Scans a directory of installed packs, validates each one, and builds the <see cref="ContentRegistry"/>
/// the rest of the engine reads from. Takes its path in the constructor the same way
/// <see cref="Persistence.JsonCampaignRepository"/> does - there is no container here, and this is the
/// composition root's job to wire up, not something the engine resolves for itself. <paramref
/// name="types"/> is the engine's only window into content types (see <see cref="IContentTypeCatalog"/>)
/// - the composition root hands in the aggregate over every installed content set.
/// <para>
/// The governing rule for a pack's own manifest: <b>a pack is rejected whole, and says why.</b> A
/// malformed <c>pack.json</c> throws the whole directory out - there is no identity to address its
/// contents by otherwise. A malformed entry file, in contrast, marks only that one file: see
/// <see cref="RejectedEntry"/> for a file that could not even be read as an entry, and
/// <see cref="ResolveEntries"/> for one that parsed fine but could not be bound to a content type.
/// </para>
/// <para>
/// Section 13 of the content architecture doc reduces this layer's entire security model to
/// "validate at load, plus size limits", because there is no level-3 (script) to sandbox. That is
/// why the validation below is thorough rather than merely best-effort: it is the only gate there
/// is.
/// </para>
/// </summary>
public sealed class ContentPackLoader(string packsPath, IContentTypeCatalog types)
{
    private const int CurrentFormatVersion = 1;
    private const int MaxFileBytes = 1024 * 1024;
    private const int MaxItemsPerPack = 10_000;

    private const string PackFileName = "pack.json";
    private const string EntriesDirectoryName = "entries";

    private static readonly JsonElement EmptyValues = JsonDocument.Parse("{}").RootElement.Clone();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<ContentRegistry> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(packsPath))
        {
            // The directory name never carries meaning (identity lives in pack.json), so a missing
            // directory is not "no packs are named yet" - it is simply nothing to scan.
            return new ContentRegistry([], [], [], []);
        }

        string[] directories;

        try
        {
            // Enumeration is cheap metadata work, so it stays synchronous even though everything below
            // it becomes real async IO - there is no async directory-listing API to call instead.
            directories = [.. Directory.EnumerateDirectories(packsPath).OrderBy(d => d, StringComparer.Ordinal)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The root itself is what could not be scanned, so there is no per-candidate directory to
            // blame - the whole scan is reported as a single rejection naming packsPath, rather than
            // silently returning an empty registry (which would look identical to "no packs installed")
            // or letting the exception reach the caller and crash the app it is starting up for.
            return new ContentRegistry([], [], [], [new RejectedPack(packsPath, $"could not be scanned: {ex.Message}")]);
        }

        var packs = new List<(string Location, Pack Pack, IReadOnlyList<RejectedEntry> RejectedEntries)>();
        var rejected = new List<RejectedPack>();

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = await LoadCandidateAsync(directory, cancellationToken);

            if (candidate.Pack is not null)
            {
                packs.Add((directory, candidate.Pack, candidate.RejectedEntries));
            }
            else if (candidate.Rejected is not null)
            {
                rejected.Add(candidate.Rejected);
            }
        }

        // Two installed packs sharing an id are both rejected. A collision is invisible until every
        // candidate directory has been read, so it can only be checked here, after the scan.
        var collidingIds = packs
            .Select(entry => entry.Pack.Id)
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        if (collidingIds.Count > 0)
        {
            foreach (var (location, pack, _) in packs.Where(entry => collidingIds.Contains(entry.Pack.Id)))
            {
                rejected.Add(new RejectedPack(location, $"id '{pack.Id}' is used by more than one installed pack."));
            }

            // A pack rejected here was read fully - manifest and entries alike - before the collision
            // became visible. Its RejectedEntry items must not reach the registry either: the whole
            // directory is out, entries included, so they are dropped along with the pack itself
            // rather than flattened into the loop below.
            packs.RemoveAll(entry => collidingIds.Contains(entry.Pack.Id));
        }

        var registeredEntries = new List<RegisteredEntry>();
        var rejectedEntries = new List<RejectedEntry>();

        foreach (var (_, pack, packRejectedEntries) in packs)
        {
            registeredEntries.AddRange(ResolveEntries(pack.Id, pack.Entries, types));
            rejectedEntries.AddRange(packRejectedEntries);
        }

        return new ContentRegistry(
            [.. packs.Select(entry => entry.Pack)],
            [.. registeredEntries],
            [.. rejectedEntries],
            [.. rejected]);
    }

    /// <summary>
    /// Loads and fully validates one candidate directory's manifest, in isolation. Every failure about
    /// the manifest itself - a missing file, a syntax error, a stray key, an invalid id, name or
    /// version - funnels through <see cref="PackRejectedException"/> so this method has exactly one
    /// exit for "this directory is not installable". Failures scoped to a single entry file never
    /// reach that exit: <see cref="LoadEntriesAsync"/> catches them per file and returns them
    /// as <see cref="RejectedEntry"/> instead, alongside the pack that keeps loading regardless.
    /// <para>
    /// A <c>templates</c> directory, if present, is silently ignored rather than inspected or rejected
    /// for - content types no longer come from packs, so a leftover or forgotten one is inert, not an
    /// error. Rejecting a whole pack for a directory nobody reads from would contradict the very rule
    /// (section 13) that one defect must never hide every entry beside it.
    /// </para>
    /// <para>
    /// An <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> reaching this method -
    /// the manifest vanishing between enumeration and read, a locked <c>pack.json</c>, the entries
    /// directory itself becoming unlistable - rejects this one candidate whole, naming the file or
    /// directory that could not be read, and every sibling pack keeps loading normally. The same
    /// failure inside a single entry file never surfaces here at all, because
    /// <see cref="LoadEntriesAsync"/> already turned it into a <see cref="RejectedEntry"/> before this
    /// method's own try/catch could see it.
    /// </para>
    /// </summary>
    private async Task<LoadCandidateResult> LoadCandidateAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            var packPath = Path.Combine(directory, PackFileName);

            if (!File.Exists(packPath))
            {
                throw new PackRejectedException($"'{PackFileName}' is missing.");
            }

            var packDto = await ParseStrictAsync<PackFileDto>(packPath, PackFileName, cancellationToken);

            if (packDto.FormatVersion is null || packDto.FormatVersion != CurrentFormatVersion)
            {
                throw new PackRejectedException($"'{PackFileName}' declares unknown formatVersion '{packDto.FormatVersion}'.");
            }

            if (!ContentId.TryCreate(packDto.Id, out var packId))
            {
                throw new PackRejectedException($"'{PackFileName}' has an invalid id '{packDto.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(packDto.Name))
            {
                throw new PackRejectedException($"'{PackFileName}' is missing a name.");
            }

            if (packDto.Version?.Major is not { } major || major < 0
                || packDto.Version.Minor is not { } minor || minor < 0)
            {
                throw new PackRejectedException($"'{PackFileName}' has an invalid version.");
            }

            var version = new PackVersion(major, minor);
            var entriesDirectory = Path.Combine(directory, EntriesDirectoryName);
            var (entries, rejectedEntries) = await LoadEntriesAsync(entriesDirectory, packId, cancellationToken);

            return new LoadCandidateResult(new Pack(packId, packDto.Name.Trim(), version, entries), rejectedEntries, null);
        }
        catch (PackRejectedException ex)
        {
            return new LoadCandidateResult(null, [], new RejectedPack(directory, ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Catches what no inner try/catch already turned into a PackRejectedException: entries
            // directory enumeration below failing outright, or any other IO surprise. The BCL's own
            // exception messages already name the offending path, so no extra bookkeeping is needed to
            // satisfy "the reason names the file or directory".
            return new LoadCandidateResult(null, [], new RejectedPack(directory, $"could not be read: {ex.Message}"));
        }
    }

    /// <summary>
    /// One candidate directory's outcome. Exactly one of <see cref="Pack"/> or <see cref="Rejected"/>
    /// is non-null. <see cref="RejectedEntries"/> is only ever populated alongside <see cref="Pack"/> -
    /// a rejected candidate has no pack id to attribute its entries to, and never got far enough to
    /// read them as anything but part of the manifest failure.
    /// </summary>
    private sealed record LoadCandidateResult(Pack? Pack, IReadOnlyList<RejectedEntry> RejectedEntries, RejectedPack? Rejected);

    /// <summary>
    /// Reads every <c>*.json</c> file in one pack's <c>entries</c> directory and splits the result into
    /// what is registrable and what is not, per file. Two things still reject the whole pack, because
    /// they are properties of the directory as a whole rather than a defect in any one file: the
    /// directory itself failing to enumerate (an <see cref="IOException"/> or
    /// <see cref="UnauthorizedAccessException"/> propagates straight out to <see cref="LoadCandidateAsync"/>),
    /// and more than <see cref="MaxItemsPerPack"/> files being present.
    /// <para>
    /// Everything else about a single file - too large, unreadable, not valid JSON, an unknown key, an
    /// invalid or missing id, a missing name, an invalid template reference, a missing template version -
    /// is caught here, per file, and turned into a <see cref="RejectedEntry"/> rather than aborting the
    /// pack. Every other file keeps loading regardless of what any one of them did.
    /// </para>
    /// <para>
    /// A duplicated entry id is only visible once every file has been parsed, so it is detected in a
    /// second pass over the results, mirroring how the pack-id collision in <see cref="LoadAsync"/> is
    /// only checked once every candidate directory has been read. Every file declaring the colliding id
    /// is rejected - not just the second one to appear - because a <see cref="RegisteredEntry"/> carries
    /// no file name to tell colliding files apart by, and letting either through would put two entries
    /// under the same <see cref="EntryAddress"/>, so the address would stop addressing.
    /// </para>
    /// </summary>
    private async Task<(IReadOnlyList<Entry> Entries, IReadOnlyList<RejectedEntry> RejectedEntries)> LoadEntriesAsync(
        string entriesDirectory, ContentId packId, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(entriesDirectory))
        {
            return ([], []);
        }

        var files = Directory.EnumerateFiles(entriesDirectory, "*.json")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        if (files.Length > MaxItemsPerPack)
        {
            throw new PackRejectedException($"pack '{packId}' declares more than {MaxItemsPerPack} entries.");
        }

        var outcomes = new List<EntryFileOutcome>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var label = RelativeLabel(EntriesDirectoryName, file);

            try
            {
                var dto = await ParseStrictAsync<EntryFileDto>(file, label, cancellationToken);
                var entry = ParseEntry(dto, label);
                outcomes.Add(new EntryFileOutcome(label, entry, null));
            }
            catch (PackRejectedException ex)
            {
                outcomes.Add(new EntryFileOutcome(label, null, ex.Message));
            }
        }

        // Keyed by the colliding id, valued with every file declaring it: the reason has to name all
        // of them, not just the file it is attached to. "declared more than once" on one file alone
        // reads as an accusation against that file, when the defect is the pair.
        var duplicates = outcomes
            .Where(outcome => outcome.Entry is not null)
            .GroupBy(outcome => outcome.Entry!.Id)
            .Where(group => group.Count() > 1)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group.Select(outcome => $"'{outcome.Label}'")));

        var entries = new List<Entry>();
        var rejectedEntries = new List<RejectedEntry>();

        foreach (var outcome in outcomes)
        {
            if (outcome.FailureReason is { } reason)
            {
                rejectedEntries.Add(new RejectedEntry(packId, outcome.Label, reason));
            }
            else if (duplicates.TryGetValue(outcome.Entry!.Id, out var collidingFiles))
            {
                rejectedEntries.Add(new RejectedEntry(packId, outcome.Label,
                    $"pack '{packId}' declares entry id '{outcome.Entry.Id}' in more than one file: {collidingFiles}."));
            }
            else
            {
                entries.Add(outcome.Entry!);
            }
        }

        return (entries, rejectedEntries);
    }

    /// <summary>One entry file's parse attempt: either an <see cref="Entry"/>, or the reason it failed.</summary>
    private readonly record struct EntryFileOutcome(string Label, Entry? Entry, string? FailureReason);

    private Entry ParseEntry(EntryFileDto dto, string label)
    {
        if (!ContentId.TryCreate(dto.Id, out var id))
        {
            throw new PackRejectedException($"'{label}' has an invalid id '{dto.Id}'.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new PackRejectedException($"'{label}' is missing a name.");
        }

        if (!ContentTypeReference.TryParse(dto.Template, out var typeReference))
        {
            throw new PackRejectedException($"'{label}' has an invalid template reference '{dto.Template}'.");
        }

        if (dto.TemplateVersion is not { } typeVersion)
        {
            throw new PackRejectedException($"'{label}' is missing templateVersion.");
        }

        var valuesElement = dto.Values.ValueKind == JsonValueKind.Undefined ? EmptyValues : dto.Values;

        return new Entry(id, dto.Name.Trim(), typeReference, typeVersion, new ContentValues(valuesElement));
    }

    /// <summary>
    /// Resolves every entry in one pack against <paramref name="types"/>, in one pass:
    /// <see cref="IContentTypeCatalog.HasSet"/>, then <see cref="IContentTypeCatalog.TryGet"/>, then
    /// the version, then <see cref="IContentTypeCatalog.TryValidate"/>. Every one of the four ways an
    /// entry can fail to resolve marks only that entry (<see cref="EntryUnresolvedReason"/>) - none of
    /// them reject the pack itself, which is exactly the "Odrzucanie całej paczki za jeden wadliwy
    /// wpis" reversal docs/decisions.md records: a content set that rejects one entry's values says
    /// nothing about any other entry beside it.
    /// </summary>
    private static IEnumerable<RegisteredEntry> ResolveEntries(
        ContentId packId, IReadOnlyList<Entry> entries, IContentTypeCatalog types)
    {
        foreach (var entry in entries)
        {
            var address = new EntryAddress(packId, entry.Id);

            if (!types.HasSet(entry.Type.Set))
            {
                yield return RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.MissingSet);
                continue;
            }

            if (!types.TryGet(entry.Type, out var descriptor))
            {
                yield return RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.MissingType);
                continue;
            }

            if (descriptor.Version != entry.TypeVersion)
            {
                yield return RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.TypeVersionMismatch);
                continue;
            }

            if (!types.TryValidate(entry.Type, entry.Values, out var error))
            {
                yield return RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.ValuesRejected, error);
                continue;
            }

            yield return RegisteredEntry.CreateResolved(address, entry, descriptor);
        }
    }

    private async Task<TDto> ParseStrictAsync<TDto>(string path, string label, CancellationToken cancellationToken)
    {
        string text;

        try
        {
            // The size check lives inside this same guarded region as the read itself: both touch the
            // same file, and a file that vanishes or becomes locked between the two calls must reject
            // this one unit of work - the whole pack for a manifest, just the one file for an entry -
            // rather than crash the whole scan (see the class-level remarks).
            if (new FileInfo(path).Length > MaxFileBytes)
            {
                throw new PackRejectedException($"'{label}' exceeds the {MaxFileBytes}-byte size limit.");
            }

            text = await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // UnauthorizedAccessException does not derive from IOException, so it needs its own arm
            // here - a directory or file this process cannot read is exactly as "rejected, not fatal"
            // as a missing or malformed one.
            throw new PackRejectedException($"'{label}' could not be read: {ex.Message}");
        }

        try
        {
            return JsonSerializer.Deserialize<TDto>(text, _options)
                ?? throw new PackRejectedException($"'{label}' is empty.");
        }
        catch (JsonException ex)
        {
            throw new PackRejectedException($"'{label}' is not valid: {ex.Message}");
        }
    }

    private static string RelativeLabel(string subDirectoryName, string filePath) =>
        $"{subDirectoryName}/{Path.GetFileName(filePath)}";

    /// <summary>
    /// Internal control flow only: every manifest or entry-file validation failure below throws one of
    /// these. Two places catch it, one per rejection scope: <see cref="LoadCandidateAsync"/> for a
    /// failure about the manifest or the entries directory as a whole, turning it into a
    /// <see cref="RejectedPack"/>; <see cref="LoadEntriesAsync"/> for a failure scoped to a single
    /// entry file, turning it into a <see cref="RejectedEntry"/> without letting it reach
    /// <see cref="LoadCandidateAsync"/> at all. It never crosses out of this class, so callers of
    /// <see cref="LoadAsync"/> never see an exception for a malformed pack or entry - they see it show
    /// up in <see cref="ContentRegistry.RejectedPacks"/> or <see cref="ContentRegistry.RejectedEntries"/>
    /// instead.
    /// </summary>
    private sealed class PackRejectedException(string reason) : Exception(reason);

    private sealed record PackFileDto(int? FormatVersion, string? Id, string? Name, PackVersionDto? Version);

    private sealed record PackVersionDto(int? Major, int? Minor);

    // "Template"/"TemplateVersion" on the wire, unchanged, matching the entry file format
    // docs/architecture.md's "Deklaracja treści" section already shows - only the in-memory record
    // (Entry.Type / Entry.TypeVersion) took the new name.
    private sealed record EntryFileDto(string? Id, string? Name, string? Template, int? TemplateVersion, JsonElement Values);
}
