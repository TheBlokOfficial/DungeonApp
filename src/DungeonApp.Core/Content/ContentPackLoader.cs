using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
/// composition root's job to wire up, not something the engine resolves for itself.
/// <para>
/// The governing rule, repeated everywhere below: <b>a pack is rejected whole, and says why.</b> One
/// malformed file never partially loads - the whole directory it lives in is set aside - and one
/// rejected pack never stops any other pack in <paramref name="packsPath"/> from loading normally.
/// </para>
/// <para>
/// Section 13 of the content architecture doc reduces this layer's entire security model to
/// "validate at load, plus size limits", because there is no level-3 (script) to sandbox. That is
/// why the validation below is thorough rather than merely best-effort: it is the only gate there
/// is.
/// </para>
/// </summary>
public sealed class ContentPackLoader(string packsPath)
{
    private const int CurrentFormatVersion = 1;
    private const int MaxFileBytes = 1024 * 1024;
    private const int MaxItemsPerPack = 10_000;

    private const string PackFileName = "pack.json";
    private const string TemplatesDirectoryName = "templates";
    private const string EntriesDirectoryName = "entries";

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

        var systemPacks = new List<(string Location, SystemPack Pack)>();
        var contentPacks = new List<(string Location, ContentPack Pack)>();
        var rejected = new List<RejectedPack>();

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (system, content, rejectedPack) = await LoadCandidateAsync(directory, cancellationToken);

            if (system is not null)
            {
                systemPacks.Add((directory, system));
            }
            else if (content is not null)
            {
                contentPacks.Add((directory, content));
            }
            else if (rejectedPack is not null)
            {
                rejected.Add(rejectedPack);
            }
        }

        // Rule 7: two installed packs sharing an id are both rejected. A collision is invisible until
        // every candidate directory has been read, so it can only be checked here, after the scan.
        var collidingIds = systemPacks.Select(entry => entry.Pack.Id)
            .Concat(contentPacks.Select(entry => entry.Pack.Id))
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        if (collidingIds.Count > 0)
        {
            foreach (var (location, pack) in systemPacks.Where(entry => collidingIds.Contains(entry.Pack.Id)))
            {
                rejected.Add(new RejectedPack(location, $"id '{pack.Id}' is used by more than one installed pack."));
            }

            foreach (var (location, pack) in contentPacks.Where(entry => collidingIds.Contains(entry.Pack.Id)))
            {
                rejected.Add(new RejectedPack(location, $"id '{pack.Id}' is used by more than one installed pack."));
            }

            systemPacks.RemoveAll(entry => collidingIds.Contains(entry.Pack.Id));
            contentPacks.RemoveAll(entry => collidingIds.Contains(entry.Pack.Id));
        }

        // Cross-pack resolution (entries against templates in another pack) can only happen now that
        // every surviving pack is known - a template a content pack names might live in a system pack
        // that has not been read yet at the point the content pack itself was parsed.
        var systemPacksById = systemPacks.ToDictionary(entry => entry.Pack.Id, entry => entry.Pack);
        var contentPackIds = contentPacks.Select(entry => entry.Pack.Id).ToHashSet();

        var finalContentPacks = new List<ContentPack>();
        var registeredEntries = new List<RegisteredEntry>();

        foreach (var (location, pack) in contentPacks)
        {
            if (TryResolveContentPack(pack, systemPacksById, contentPackIds, out var entries, out var rejectionReason))
            {
                finalContentPacks.Add(pack);
                registeredEntries.AddRange(entries);
            }
            else
            {
                rejected.Add(new RejectedPack(location, rejectionReason!));
            }
        }

        var registry = new ContentRegistry(
            [.. systemPacks.Select(entry => entry.Pack)],
            [.. finalContentPacks],
            [.. registeredEntries],
            [.. rejected]);

        return registry;
    }

    /// <summary>
    /// Loads and fully validates one candidate directory in isolation. Every failure anywhere inside
    /// - a missing file, a syntax error, a stray key, a dangling reference - funnels through
    /// <see cref="PackRejectedException"/> so this method has exactly one exit for "this directory is
    /// not installable", instead of a validation step for every rule above.
    /// <para>
    /// An <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> reaching here - a file
    /// vanishing between enumeration and read, a locked file, a directory this process cannot list -
    /// is folded into the same outcome: this one candidate is rejected, naming the file or directory
    /// that could not be read, and every sibling pack keeps loading normally.
    /// </para>
    /// </summary>
    private async Task<(SystemPack? System, ContentPack? Content, RejectedPack? Rejected)> LoadCandidateAsync(
        string directory, CancellationToken cancellationToken)
    {
        try
        {
            var packPath = Path.Combine(directory, PackFileName);

            if (!File.Exists(packPath))
            {
                throw new PackRejectedException($"'{PackFileName}' is missing.");
            }

            var pack = await ParseStrictAsync<PackFileDto>(packPath, PackFileName, cancellationToken);

            if (pack.FormatVersion is null || pack.FormatVersion != CurrentFormatVersion)
            {
                throw new PackRejectedException($"'{PackFileName}' declares unknown formatVersion '{pack.FormatVersion}'.");
            }

            if (!ContentId.TryCreate(pack.Id, out var packId))
            {
                throw new PackRejectedException($"'{PackFileName}' has an invalid id '{pack.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(pack.Name))
            {
                throw new PackRejectedException($"'{PackFileName}' is missing a name.");
            }

            if (pack.Version?.Major is not { } major || major < 0
                || pack.Version.Minor is not { } minor || minor < 0)
            {
                throw new PackRejectedException($"'{PackFileName}' has an invalid version.");
            }

            var version = new PackVersion(major, minor);
            var templatesDirectory = Path.Combine(directory, TemplatesDirectoryName);
            var entriesDirectory = Path.Combine(directory, EntriesDirectoryName);
            var hasTemplates = Directory.Exists(templatesDirectory);
            var hasEntries = Directory.Exists(entriesDirectory);

            switch (pack.Kind)
            {
                case "system":
                    // A system pack brings shape, never content - see section 11's ban on anything but
                    // the 3-to-2 dependency edge.
                    if (hasEntries)
                    {
                        throw new PackRejectedException(
                            $"system pack '{packId}' must not contain an '{EntriesDirectoryName}' directory.");
                    }

                    return (
                        await LoadTemplatesAsync(templatesDirectory, hasTemplates, packId, pack.Name.Trim(), version, cancellationToken),
                        null,
                        null);

                case "content":
                    if (hasTemplates)
                    {
                        throw new PackRejectedException(
                            $"content pack '{packId}' must not contain a '{TemplatesDirectoryName}' directory.");
                    }

                    return (
                        null,
                        await LoadEntriesAsync(entriesDirectory, hasEntries, packId, pack.Name.Trim(), version, cancellationToken),
                        null);

                default:
                    throw new PackRejectedException($"'{PackFileName}' declares an unknown kind '{pack.Kind}'.");
            }
        }
        catch (PackRejectedException ex)
        {
            return (null, null, new RejectedPack(directory, ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Catches what no inner try/catch already turned into a PackRejectedException: directory
            // enumeration below (templates/entries) failing outright, or any other IO surprise. The
            // BCL's own exception messages already name the offending path, so no extra bookkeeping
            // is needed to satisfy "the reason names the file or directory".
            return (null, null, new RejectedPack(directory, $"could not be read: {ex.Message}"));
        }
    }

    private async Task<SystemPack> LoadTemplatesAsync(
        string templatesDirectory, bool hasTemplates, ContentId packId, string name, PackVersion version,
        CancellationToken cancellationToken)
    {
        if (!hasTemplates)
        {
            return new SystemPack(packId, name, version, []);
        }

        var files = Directory.EnumerateFiles(templatesDirectory, "*.json")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        if (files.Length > MaxItemsPerPack)
        {
            throw new PackRejectedException($"system pack '{packId}' declares more than {MaxItemsPerPack} templates.");
        }

        var templates = new List<Template>();
        var seenIds = new HashSet<ContentId>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var label = RelativeLabel(TemplatesDirectoryName, file);
            var dto = await ParseStrictAsync<TemplateFileDto>(file, label, cancellationToken);
            var template = ParseTemplate(dto, label);

            if (!seenIds.Add(template.Id))
            {
                throw new PackRejectedException(
                    $"system pack '{packId}' declares template id '{template.Id}' more than once (in '{label}').");
            }

            templates.Add(template);
        }

        return new SystemPack(packId, name, version, [.. templates]);
    }

    private async Task<ContentPack> LoadEntriesAsync(
        string entriesDirectory, bool hasEntries, ContentId packId, string name, PackVersion version,
        CancellationToken cancellationToken)
    {
        if (!hasEntries)
        {
            return new ContentPack(packId, name, version, []);
        }

        var files = Directory.EnumerateFiles(entriesDirectory, "*.json")
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        if (files.Length > MaxItemsPerPack)
        {
            throw new PackRejectedException($"content pack '{packId}' declares more than {MaxItemsPerPack} entries.");
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
                    $"content pack '{packId}' declares entry id '{entry.Id}' more than once (in '{label}').");
            }

            entries.Add(entry);
        }

        return new ContentPack(packId, name, version, [.. entries]);
    }

    private Template ParseTemplate(TemplateFileDto dto, string label)
    {
        if (!ContentId.TryCreate(dto.Id, out var id))
        {
            throw new PackRejectedException($"'{label}' has an invalid id '{dto.Id}'.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new PackRejectedException($"'{label}' is missing a name.");
        }

        if (dto.Version is not { } version || version < 1)
        {
            throw new PackRejectedException($"'{label}' has an invalid version.");
        }

        if (dto.CatalogVersion is not { } catalogVersion || catalogVersion < 1)
        {
            throw new PackRejectedException($"'{label}' has an invalid catalogVersion.");
        }

        var fields = new List<FieldDeclaration>();
        var fieldsByName = new Dictionary<FieldName, FieldDeclaration>();

        foreach (var fieldDto in dto.Fields ?? [])
        {
            if (!FieldName.TryCreate(fieldDto.Id, out var fieldId))
            {
                throw new PackRejectedException($"'{label}' has a field with an invalid id '{fieldDto.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(fieldDto.Label))
            {
                throw new PackRejectedException($"'{label}' field '{fieldId}' is missing a label.");
            }

            var type = fieldDto.Type switch
            {
                "text" => FieldType.Text,
                "integer" => FieldType.Integer,
                _ => throw new PackRejectedException($"'{label}' field '{fieldId}' has an unknown type '{fieldDto.Type}'.")
            };

            var declaration = new FieldDeclaration(fieldId, fieldDto.Label.Trim(), type, fieldDto.Required ?? true);

            if (!fieldsByName.TryAdd(fieldId, declaration))
            {
                throw new PackRejectedException($"'{label}' declares field id '{fieldId}' more than once.");
            }

            fields.Add(declaration);
        }

        var card = new List<CardElement>();
        var cardDtos = dto.Card ?? [];

        for (var index = 0; index < cardDtos.Count; index++)
        {
            card.Add(ParseCardElement(cardDtos[index], index, label, fieldsByName));
        }

        return new Template(id, dto.Name.Trim(), version, catalogVersion, [.. fields], [.. card]);
    }

    private CardElement ParseCardElement(
        JsonElement raw, int index, string label, IReadOnlyDictionary<FieldName, FieldDeclaration> fields)
    {
        if (!raw.TryGetProperty("element", out var elementProperty) || elementProperty.ValueKind != JsonValueKind.String)
        {
            throw new PackRejectedException($"'{label}' card element #{index} is missing 'element'.");
        }

        var elementName = elementProperty.GetString();

        return elementName switch
        {
            "statblock" => ParseStatblockElement(raw, index, label, fields),
            "prose" => ParseProseElement(raw, index, label, fields),
            _ => throw new PackRejectedException($"'{label}' card element #{index} has an unknown element type '{elementName}'.")
        };
    }

    private StatblockElement ParseStatblockElement(
        JsonElement raw, int index, string label, IReadOnlyDictionary<FieldName, FieldDeclaration> fields)
    {
        var dto = DeserializeCardElement<StatblockElementDto>(raw, index, label);
        var traits = new List<StatblockTrait>();

        foreach (var traitDto in dto.Traits ?? [])
        {
            if (!FieldName.TryCreate(traitDto.Field, out var field))
            {
                throw new PackRejectedException(
                    $"'{label}' card element #{index} has a trait with an invalid field '{traitDto.Field}'.");
            }

            if (!fields.ContainsKey(field))
            {
                throw new PackRejectedException(
                    $"'{label}' card element #{index} references undeclared field '{field}'.");
            }

            FieldName? secondary = null;

            if (traitDto.Secondary is not null)
            {
                if (!FieldName.TryCreate(traitDto.Secondary, out var secondaryField))
                {
                    throw new PackRejectedException(
                        $"'{label}' card element #{index} has a trait with an invalid secondary field '{traitDto.Secondary}'.");
                }

                if (!fields.ContainsKey(secondaryField))
                {
                    throw new PackRejectedException(
                        $"'{label}' card element #{index} references undeclared secondary field '{secondaryField}'.");
                }

                secondary = secondaryField;
            }

            traits.Add(new StatblockTrait(field, secondary));
        }

        return new StatblockElement(dto.Title, [.. traits], dto.Compact ?? false);
    }

    private ProseElement ParseProseElement(
        JsonElement raw, int index, string label, IReadOnlyDictionary<FieldName, FieldDeclaration> fields)
    {
        var dto = DeserializeCardElement<ProseElementDto>(raw, index, label);

        if (!FieldName.TryCreate(dto.Field, out var field))
        {
            throw new PackRejectedException($"'{label}' card element #{index} has an invalid field '{dto.Field}'.");
        }

        if (!fields.ContainsKey(field))
        {
            throw new PackRejectedException($"'{label}' card element #{index} references undeclared field '{field}'.");
        }

        return new ProseElement(dto.Title, field);
    }

    /// <summary>
    /// Re-deserializes one card element's raw JSON into its variant-specific DTO, so an unknown key
    /// on (say) a "prose" element - like a stray "traits" copied from a statblock - is caught by the
    /// same strict, unmapped-member check every other file goes through, rather than silently
    /// accepted because the union of every variant's keys would have covered it.
    /// </summary>
    private TDto DeserializeCardElement<TDto>(JsonElement raw, int index, string label)
    {
        try
        {
            return JsonSerializer.Deserialize<TDto>(raw.GetRawText(), _options)
                ?? throw new PackRejectedException($"'{label}' card element #{index} is empty.");
        }
        catch (JsonException ex)
        {
            throw new PackRejectedException($"'{label}' card element #{index}: {ex.Message}");
        }
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

        if (!TemplateReference.TryParse(dto.Template, out var templateReference))
        {
            throw new PackRejectedException($"'{label}' has an invalid template reference '{dto.Template}'.");
        }

        if (dto.TemplateVersion is not { } templateVersion)
        {
            throw new PackRejectedException($"'{label}' is missing templateVersion.");
        }

        var values = new Dictionary<FieldName, FieldValue>();

        foreach (var (key, element) in dto.Values ?? [])
        {
            if (!FieldName.TryCreate(key, out var fieldName))
            {
                throw new PackRejectedException($"'{label}' has an invalid field name '{key}' in values.");
            }

            FieldValue value = element.ValueKind switch
            {
                JsonValueKind.String => new TextValue(element.GetString() ?? string.Empty),
                JsonValueKind.Number when element.TryGetInt64(out var integer) => new IntegerValue(integer),
                JsonValueKind.Number => throw new PackRejectedException(
                    $"'{label}' field '{fieldName}' is not a whole number."),
                _ => throw new PackRejectedException($"'{label}' field '{fieldName}' has an unsupported value type.")
            };

            if (!values.TryAdd(fieldName, value))
            {
                throw new PackRejectedException($"'{label}' declares field '{fieldName}' more than once in values.");
            }
        }

        return new Entry(id, dto.Name.Trim(), templateReference, templateVersion, values.ToImmutableDictionary());
    }

    /// <summary>
    /// Resolves every entry in one content pack against the system packs installed alongside it.
    /// Returns <c>false</c> when the pack itself must be rejected - either it names a template that
    /// lives in another content pack (the forbidden 3-to-2 edge, section 11) or one of its entries
    /// contradicts the template it did resolve against. Everything else (a missing pack, a missing
    /// template, a version bump) leaves the pack installed with that one entry marked unresolved
    /// instead.
    /// </summary>
    private static bool TryResolveContentPack(
        ContentPack pack,
        IReadOnlyDictionary<ContentId, SystemPack> systemPacksById,
        IReadOnlySet<ContentId> contentPackIds,
        out List<RegisteredEntry> entries,
        out string? rejectionReason)
    {
        entries = new List<RegisteredEntry>();

        foreach (var entry in pack.Entries)
        {
            var address = new EntryAddress(pack.Id, entry.Id);
            var templatePackId = entry.Template.Pack;

            if (systemPacksById.TryGetValue(templatePackId, out var systemPack))
            {
                var template = systemPack.Templates.FirstOrDefault(candidate => candidate.Id == entry.Template.Template);

                if (template is null)
                {
                    entries.Add(RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.MissingTemplate));
                    continue;
                }

                if (template.Version != entry.TemplateVersion)
                {
                    entries.Add(RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.TemplateVersionMismatch));
                    continue;
                }

                if (!TryValidateValues(entry, template, out var valueError))
                {
                    rejectionReason = $"entry '{entry.Id}': {valueError}";
                    entries = [];
                    return false;
                }

                entries.Add(RegisteredEntry.CreateResolved(address, entry, template));
                continue;
            }

            if (contentPackIds.Contains(templatePackId))
            {
                rejectionReason =
                    $"entry '{entry.Id}' names template '{entry.Template}', which lives in a content pack, not a system pack.";
                entries = [];
                return false;
            }

            entries.Add(RegisteredEntry.CreateUnresolved(address, entry, EntryUnresolvedReason.MissingPack));
        }

        rejectionReason = null;
        return true;
    }

    /// <summary>
    /// Checks one resolved entry against the template it is now bound to: every required field
    /// present, every present value's runtime kind matching what the field declares, and no value
    /// left over for a field the template never declared.
    /// </summary>
    private static bool TryValidateValues(Entry entry, Template template, out string? error)
    {
        foreach (var field in template.Fields)
        {
            if (!entry.Values.TryGetValue(field.Id, out var value))
            {
                if (field.Required)
                {
                    error = $"missing required field '{field.Id}'.";
                    return false;
                }

                continue;
            }

            var typeMatches = field.Type switch
            {
                FieldType.Text => value is TextValue,
                FieldType.Integer => value is IntegerValue,
                _ => false
            };

            if (!typeMatches)
            {
                error = $"field '{field.Id}' has a value that does not match its declared type.";
                return false;
            }
        }

        var declaredFieldIds = template.Fields.Select(field => field.Id).ToHashSet();

        foreach (var fieldName in entry.Values.Keys)
        {
            if (!declaredFieldIds.Contains(fieldName))
            {
                error = $"field '{fieldName}' is not declared by template '{template.Id}'.";
                return false;
            }
        }

        error = null;
        return true;
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
    /// Internal control flow only: every validation failure below throws one of these, and
    /// <see cref="LoadCandidateAsync"/> is the only place that catches it. It never crosses that
    /// boundary, so callers of <see cref="LoadAsync"/> never see an exception for a malformed pack -
    /// they see it show up in <see cref="ContentRegistry.RejectedPacks"/> instead.
    /// </summary>
    private sealed class PackRejectedException(string reason) : Exception(reason);

    private sealed record PackFileDto(int? FormatVersion, string? Id, string? Kind, string? Name, PackVersionDto? Version);

    private sealed record PackVersionDto(int? Major, int? Minor);

    private sealed record TemplateFileDto(
        string? Id,
        string? Name,
        int? Version,
        int? CatalogVersion,
        List<FieldDeclarationDto>? Fields,
        List<JsonElement>? Card);

    private sealed record FieldDeclarationDto(string? Id, string? Label, string? Type, bool? Required);

    private sealed record StatblockElementDto(string? Element, string? Title, List<StatblockTraitDto>? Traits, bool? Compact);

    private sealed record StatblockTraitDto(string? Field, string? Secondary);

    private sealed record ProseElementDto(string? Element, string? Title, string? Field);

    private sealed record EntryFileDto(
        string? Id,
        string? Name,
        string? Template,
        int? TemplateVersion,
        Dictionary<string, JsonElement>? Values);
}
