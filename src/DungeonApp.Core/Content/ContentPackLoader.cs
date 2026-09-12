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
/// contents by otherwise. A malformed entry file, in contrast, marks only that one entry: see
/// <see cref="ResolveEntries"/>.
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
            return new ContentRegistry([], [], []);
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
            return new ContentRegistry([], [], [new RejectedPack(packsPath, $"could not be scanned: {ex.Message}")]);
        }

        var packs = new List<(string Location, Pack Pack)>();
        var rejected = new List<RejectedPack>();

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (pack, rejectedPack) = await LoadCandidateAsync(directory, cancellationToken);

            if (pack is not null)
            {
                packs.Add((directory, pack));
            }
            else if (rejectedPack is not null)
            {
                rejected.Add(rejectedPack);
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
            foreach (var (location, pack) in packs.Where(entry => collidingIds.Contains(entry.Pack.Id)))
            {
                rejected.Add(new RejectedPack(location, $"id '{pack.Id}' is used by more than one installed pack."));
            }

            packs.RemoveAll(entry => collidingIds.Contains(entry.Pack.Id));
        }

        var registeredEntries = new List<RegisteredEntry>();

        foreach (var (_, pack) in packs)
        {
            registeredEntries.AddRange(ResolveEntries(pack.Id, pack.Entries, types));
        }

        return new ContentRegistry(
            [.. packs.Select(entry => entry.Pack)],
            [.. registeredEntries],
            [.. rejected]);
    }

    /// <summary>
    /// Loads and fully validates one candidate directory's manifest and entries, in isolation. Every
    /// failure anywhere inside - a missing file, a syntax error, a stray key, a duplicated id - funnels
    /// through <see cref="PackRejectedException"/> so this method has exactly one exit for "this
    /// directory is not installable".
    /// <para>
    /// A <c>templates</c> directory, if present, is silently ignored rather than inspected or rejected
    /// for - content types no longer come from packs, so a leftover or forgotten one is inert, not an
    /// error. Rejecting a whole pack for a directory nobody reads from would contradict the very rule
    /// (section 13) that one defect must never hide every entry beside it.
    /// </para>
    /// <para>
    /// An <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> reaching here - a file
    /// vanishing between enumeration and read, a locked file, a directory this process cannot list -
    /// is folded into the same outcome: this one candidate is rejected, naming the file or directory
    /// that could not be read, and every sibling pack keeps loading normally.
    /// </para>
    /// </summary>
    private async Task<(Pack? Pack, RejectedPack? Rejected)> LoadCandidateAsync(
        string directory, CancellationToken cancellationToken)
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
            var entries = await LoadEntriesAsync(entriesDirectory, packId, cancellationToken);

            return (new Pack(packId, packDto.Name.Trim(), version, entries), null);
        }
        catch (PackRejectedException ex)
        {
            return (null, new RejectedPack(directory, ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Catches what no inner try/catch already turned into a PackRejectedException: entries
            // directory enumeration below failing outright, or any other IO surprise. The BCL's own
            // exception messages already name the offending path, so no extra bookkeeping is needed to
            // satisfy "the reason names the file or directory".
            return (null, new RejectedPack(directory, $"could not be read: {ex.Message}"));
        }
    }

    private async Task<IReadOnlyList<Entry>> LoadEntriesAsync(
        string entriesDirectory, ContentId packId, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(entriesDirectory))
        {
            return [];
        }

        var files = Directory.EnumerateFiles(entriesDirectory, "*.json")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        if (files.Length > MaxItemsPerPack)
        {
            throw new PackRejectedException($"pack '{packId}' declares more than {MaxItemsPerPack} entries.");
        }

        var entries = new List<Entry>();
        var seenIds = new HashSet<ContentId>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var label = RelativeLabel(EntriesDirectoryName, file);
            var dto = await ParseStrictAsync<EntryFileDto>(file, label, cancellationToken);
            var entry = ParseEntry(dto, label);

            if (!seenIds.Add(entry.Id))
            {
                throw new PackRejectedException(
                    $"pack '{packId}' declares entry id '{entry.Id}' more than once (in '{label}').");
            }

            entries.Add(entry);
        }

        return entries;
    }

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
    /// Resolves every entry in one pack against <paramref name="types"/>, in the single pass section 3
    /// of the brief describes: <see cref="IContentTypeCatalog.TryGet"/>, then the version, then
    /// <see cref="IContentTypeCatalog.TryValidate"/>. Every one of the four ways an entry can fail to
    /// resolve marks only that entry (<see cref="EntryUnresolvedReason"/>) - none of them reject the
    /// pack itself, which is exactly the "Odrzucanie całej paczki za jeden wadliwy wpis" reversal
    /// docs/decisions.md records: a content set that rejects one entry's values says nothing about any
    /// other entry beside it.
    /// </summary>
    private static IEnumerable<RegisteredEntry> ResolveEntries(
        ContentId packId, IReadOnlyList<Entry> entries, IContentTypeCatalog types)
    {
        foreach (var entry in entries)
        {
            var address = new EntryAddress(packId, entry.Id);

            if (!types.TryGet(entry.Type, out var descriptor))
            {
                yield return RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.MissingSet);
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
            // this one pack rather than crash the whole scan (see the class-level remarks).
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
    /// these, and <see cref="LoadCandidateAsync"/> is the only place that catches it. It never crosses
    /// that boundary, so callers of <see cref="LoadAsync"/> never see an exception for a malformed pack
    /// - they see it show up in <see cref="ContentRegistry.RejectedPacks"/> instead.
    /// </summary>
    private sealed class PackRejectedException(string reason) : Exception(reason);

    private sealed record PackFileDto(int? FormatVersion, string? Id, string? Name, PackVersionDto? Version);

    private sealed record PackVersionDto(int? Major, int? Minor);

    // "Template"/"TemplateVersion" on the wire, unchanged, matching the entry file format
    // docs/architecture.md's "Deklaracja treści" section already shows - only the in-memory record
    // (Entry.Type / Entry.TypeVersion) took the new name.
    private sealed record EntryFileDto(string? Id, string? Name, string? Template, int? TemplateVersion, JsonElement Values);
}
