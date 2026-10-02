using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DungeonApp.Core.Entries;

/// <summary>
/// The shape behind every content tab: given a registry and one tab's own content type profiles,
/// decides which sections and rows show for a given <see cref="ContentListState"/> and hands back
/// exactly what a view needs to draw - never a view itself, never anything Avalonia. One instance
/// serves one tab; a system with several tabs builds one <see cref="ContentListModel"/> per tab.
/// <para>
/// <b>Routing a broken entry.</b> A tab only ever shows a <em>broken</em> entry
/// (<see cref="RegisteredEntry.Unresolved"/> not null) when the entry's own declared
/// <see cref="ContentTypeReference"/> is one of this tab's own <see cref="ContentTabDefinition.ContentTypes"/>
/// - or when that reference belongs to <em>no</em> tab at all, in which case every tab shows it.
/// Telling "known type, some other tab" apart from "no tab knows this type" needs more than this
/// one tab's own definition, though: <c>allKnownTypes</c> is the union of every content
/// type every content tab the system declares, gathered once by whoever builds all the tabs. With
/// it the rule becomes one structural comparison - <c>entry.Entry.Type</c> against a set - and never
/// branches on <see cref="EntryUnresolvedReason"/> at all: a values-rejected or version-mismatched
/// entry's own reference is, by construction, one <see cref="IContentTypeCatalog.TryGet"/> already
/// recognised, so it is always in <c>allKnownTypes</c> when some tab declares it; a
/// missing-set or missing-type entry's reference never is. A <see cref="RejectedEntry"/> - a file
/// that never parsed into an <see cref="Entry"/> at all - has no reference to check and always
/// belongs to every tab, the same as a <see cref="RejectedPack"/>.
/// </para>
/// </summary>
public sealed class ContentListModel
{
    /// <summary>
    /// The library's own always-available sort, by name. Its direction ("A–Z" / "Z–A") is not part
    /// of the label: <see cref="ContentListState.SortDescending"/> carries it.
    /// </summary>
    public const string DefaultSortLabel = "Nazwa";

    /// <summary>The name of the filter over a tab's packs - the library's own, never a system's.</summary>
    public const string PackFilterLabel = "Paczka";

    /// <summary>
    /// The order every user-visible list in a content tab sorts by: pl-PL collation, case
    /// insensitive, so "Ł", "Ś", "Ż", "Ć" and friends sort next to their plain Latin neighbours
    /// instead of landing after "z" the way ordinal comparison puts them. Everything a GM actually
    /// reads as a list - the name sort, broken rows, section headers, the category and "Paczka"
    /// options - goes through this one comparer. Tie-breaking on an address or an already-ordinal
    /// value (an entry's own <see cref="EntryAddress"/>, a value filter's system-declared numeric or
    /// ranked order) is a different question and stays whatever it already was.
    /// </summary>
    private static readonly StringComparer DisplayOrder = StringComparer.Create(new CultureInfo("pl-PL"), ignoreCase: true);

    private readonly ContentTabDefinition _tab;
    private readonly IReadOnlyDictionary<ContentTypeReference, IContentTypeProfile> _profilesByType;
    private readonly IReadOnlyDictionary<ContentId, Pack> _packsById;
    private readonly IReadOnlyList<RejectedPack> _rejectedPacks;
    private readonly IReadOnlyDictionary<ContentId, IReadOnlyList<RegisteredEntry>> _validByPack;
    private readonly IReadOnlyDictionary<ContentId, IReadOnlyList<ContentBrokenRow>> _brokenByPack;

    private readonly ContentFilterOptions? _categoryFilter;
    private readonly ContentFilterOptions _packFilter;
    private readonly IReadOnlyList<ContentFilterOptions> _valueFilterOptions;
    private readonly IReadOnlyList<string> _sortLabels;
    private readonly IReadOnlyList<string> _numericSortLabels;

    public ContentListModel(
        ContentRegistry registry, ContentTabDefinition tab, IReadOnlyCollection<ContentTypeReference> allKnownTypes)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentNullException.ThrowIfNull(allKnownTypes);

        _tab = tab;
        _profilesByType = tab.ContentTypes.ToDictionary(profile => profile.Type);
        _packsById = registry.Packs.ToDictionary(pack => pack.Id);
        _rejectedPacks = registry.RejectedPacks;

        var validRows = registry.Entries
            .Where(entry => entry.Unresolved is null && _profilesByType.ContainsKey(entry.Type!.Value.Reference))
            .ToArray();

        var unresolvedRows = registry.Entries
            .Where(entry => entry.Unresolved is not null && BelongsHere(entry.Entry.Type, allKnownTypes))
            .ToArray();

        var brokenRows = unresolvedRows
            .Select(ContentBrokenRow.ForUnresolvedEntry)
            .Concat(registry.RejectedEntries.Select(ContentBrokenRow.ForRejectedFile))
            .ToArray();

        TotalCount = validRows.Length;

        _validByPack = validRows
            .GroupBy(entry => entry.Address.Pack)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<RegisteredEntry>)group.ToArray());

        _brokenByPack = brokenRows
            .GroupBy(row => row.Pack)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<ContentBrokenRow>)group.ToArray());

        // A tab mixing several content types whose categories carry different names has no consumer
        // yet: the chip takes the name of the first profile that declares one.
        var categoryLabel = tab.ContentTypes.Select(profile => profile.CategoryLabel).FirstOrDefault(label => label is not null);

        _categoryFilter = categoryLabel is null
            ? null
            : new ContentFilterOptions(
                categoryLabel,
                DistinctSorted(validRows.Select(entry => Profile(entry).Category(entry.Entry)), DisplayOrder));

        var contributingPackIds = validRows.Select(entry => entry.Address.Pack)
            .Concat(brokenRows.Select(row => row.Pack))
            .Distinct();

        var sourceOptions = contributingPackIds
            .Select(id => _packsById[id].Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, DisplayOrder)
            .Concat(_rejectedPacks.Select(pack => pack.Location).OrderBy(location => location, DisplayOrder))
            .ToArray();

        _packFilter = new ContentFilterOptions(PackFilterLabel, sourceOptions);

        var valueFilterDefinitions = tab.ContentTypes.SelectMany(profile => profile.ValueFilters).ToArray();

        _valueFilterOptions = valueFilterDefinitions
            .Select(definition => definition.Label)
            .Distinct(StringComparer.Ordinal)
            .Select(label =>
            {
                var order = valueFilterDefinitions.First(definition => definition.Label == label).OptionOrder;

                var values = validRows.Select(entry =>
                {
                    var definition = Profile(entry).ValueFilters.FirstOrDefault(candidate => candidate.Label == label);
                    return definition?.Value(entry.Entry);
                });

                return new ContentFilterOptions(label, DistinctSorted(values, order));
            })
            .ToArray();

        var sorts = tab.ContentTypes.SelectMany(profile => profile.Sorts).ToArray();
        _sortLabels = [DefaultSortLabel, .. sorts.Select(sort => sort.Label)];
        _numericSortLabels = [.. sorts.Where(sort => !sort.IsTextual).Select(sort => sort.Label)];
    }

    /// <summary>Every valid entry this tab holds, independent of any <see cref="ContentListState"/>.</summary>
    public int TotalCount { get; }

    public ContentListResult Build(ContentListState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var search = state.Search;

        bool MatchesSearch(string name) =>
            string.IsNullOrEmpty(search) || name.Contains(search, StringComparison.OrdinalIgnoreCase);

        bool PassesValueFilters(RegisteredEntry entry)
        {
            var profile = Profile(entry);

            if (!Admits(state.Categories, profile.Category(entry.Entry)))
            {
                return false;
            }

            if (state.ValueFilters is { } selections)
            {
                foreach (var definition in profile.ValueFilters)
                {
                    if (selections.TryGetValue(definition.Label, out var chosen) && !Admits(chosen, definition.Value(entry.Entry)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        var sortCompare = BuildSortComparison(state.Sort, state.SortDescending);

        var validSections = _packsById.Values
            .Select(pack =>
            {
                var valid = RowsFor(_validByPack, pack.Id)
                    .Where(entry => MatchesSearch(entry.Entry.Name) && PassesValueFilters(entry))
                    .OrderBy(entry => entry, Comparer<RegisteredEntry>.Create(sortCompare))
                    .ToArray();

                var broken = RowsFor(_brokenByPack, pack.Id)
                    .Where(row => MatchesSearch(row.DisplayName))
                    .OrderBy(row => row.DisplayName, DisplayOrder)
                    .ToArray();

                return ContentSection.ForPack(pack.Name, valid, broken);
            })
            .Where(section => section.ShownCount > 0)
            .Where(section => Admits(state.Packs, section.Header))
            .OrderBy(section => section.Header, DisplayOrder);

        var rejectedSections = _rejectedPacks
            .Select(ContentSection.ForRejectedPack)
            .Where(section => Admits(state.Packs, section.Header))
            .OrderBy(section => section.Header, DisplayOrder);

        IReadOnlyList<ContentSection> sections = [.. validSections, .. rejectedSections];

        return new ContentListResult(
            sections,
            _categoryFilter,
            _packFilter,
            _valueFilterOptions,
            _sortLabels,
            _numericSortLabels,
            TotalCount,
            ShownCount: sections.Sum(section => section.ValidRows.Count),
            Selection: ResolveSelection(state.Selected, sections));
    }

    /// <summary>
    /// One filter's test: nothing chosen admits everything, otherwise the value must be one of the
    /// chosen ones - values within a filter combine with "or". A value the entry does not have
    /// (null) is never among the chosen ones.
    /// </summary>
    private static bool Admits(IReadOnlyCollection<string>? chosen, string? value) =>
        chosen is not { Count: > 0 } || (value is not null && chosen.Contains(value));

    /// <summary>
    /// A broken entry belongs here when its own type is one this tab declares, or - the fallback for
    /// "cannot be placed" - when no tab anywhere declares it. See this type's own remarks.
    /// </summary>
    private bool BelongsHere(ContentTypeReference type, IReadOnlyCollection<ContentTypeReference> allKnownTypes) =>
        _profilesByType.ContainsKey(type) || !allKnownTypes.Contains(type);

    private IContentTypeProfile Profile(RegisteredEntry entry) => _profilesByType[entry.Type!.Value.Reference];

    private static IReadOnlyList<T> RowsFor<T>(IReadOnlyDictionary<ContentId, IReadOnlyList<T>> byPack, ContentId pack) =>
        byPack.TryGetValue(pack, out var rows) ? rows : [];

    private static IReadOnlyList<string> DistinctSorted(IEnumerable<string?> values, IComparer<string> order) =>
        values.OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(value => value, order).ToArray();

    /// <summary>
    /// The comparison valid rows within a section sort by: the chosen sort's own comparison first
    /// (zero for every entry it does not apply to, including every entry when
    /// <paramref name="sortLabel"/> is <see cref="DefaultSortLabel"/> or unknown), then name, then
    /// address - the last two giving the library's own always-available name sort both its
    /// default behaviour and every other sort's tie-break, in one comparison.
    /// <paramref name="descending"/> reverses only the chosen key - a system's key when one is
    /// chosen, otherwise the name; a tie under a reversed system key still falls back to name A–Z.
    /// </summary>
    private Comparison<RegisteredEntry> BuildSortComparison(string sortLabel, bool descending)
    {
        var definition = _tab.ContentTypes
            .SelectMany(profile => profile.Sorts)
            .FirstOrDefault(sort => sort.Label == sortLabel);
        var direction = descending ? -1 : 1;
        var nameDirection = definition is null ? direction : 1;

        return (a, b) =>
        {
            var primary = direction * (definition?.Compare(a.Entry, b.Entry) ?? 0);

            if (primary != 0)
            {
                return primary;
            }

            var byName = nameDirection * DisplayOrder.Compare(a.Entry.Name, b.Entry.Name);

            return byName != 0 ? byName : string.CompareOrdinal(a.Address.ToString(), b.Address.ToString());
        };
    }

    private ContentSelectionDetail? ResolveSelection(ContentSelectionKey? key, IReadOnlyList<ContentSection> sections)
    {
        if (key is null)
        {
            return null;
        }

        foreach (var section in sections)
        {
            if (key is EntrySelectionKey entryKey)
            {
                var valid = section.ValidRows.FirstOrDefault(row => row.Address == entryKey.Address);

                if (valid is not null)
                {
                    var profile = Profile(valid);
                    return new ValidEntrySelection(profile.Category(valid.Entry), valid.Entry.Name, profile.Tags(valid.Entry), valid);
                }

                var brokenEntry = section.BrokenRows.FirstOrDefault(
                    row => row.UnresolvedEntry is { } unresolved && unresolved.Address == entryKey.Address);

                if (brokenEntry is not null)
                {
                    return new BrokenRowSelection(brokenEntry);
                }
            }

            if (key is RejectedFileSelectionKey fileKey)
            {
                var rejectedFile = section.BrokenRows.FirstOrDefault(
                    row => row.RejectedFile is { } file && file.Pack == fileKey.Pack && file.Location == fileKey.Location);

                if (rejectedFile is not null)
                {
                    return new BrokenRowSelection(rejectedFile);
                }
            }

            if (key is RejectedPackSelectionKey packKey
                && section.RejectedPack is { } rejectedPack
                && rejectedPack.Location == packKey.Location)
            {
                return new RejectedPackSelection(rejectedPack);
            }
        }

        return null;
    }
}
