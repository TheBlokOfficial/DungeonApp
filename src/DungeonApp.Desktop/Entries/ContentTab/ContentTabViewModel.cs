using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.ViewModels;
using DungeonApp.Desktop.Entries;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// The view model behind one content tab's whole screen: wraps one <see cref="ContentListModel"/> -
/// built once, for this tab's own <see cref="ContentTabDefinition"/> - and a mutable
/// <see cref="ContentListState"/>, and exposes exactly what the skeleton view draws. Every mutation
/// (search, a filter, a sort, a selection, "Wyczyść filtry") builds the next immutable state and
/// passes it through <c>Apply</c>, the only place <see cref="ContentListModel.Build"/> is ever
/// called - the same never-change-in-place discipline campaign state follows, at list-state scale.
/// </summary>
public sealed class ContentTabViewModel : ObservableObject
{
    private readonly ContentListModel _model;
    private readonly IContentPresentation _presentation;
    private readonly IReadOnlyDictionary<ContentTypeReference, IContentTypeProfile> _profilesByType;
    private readonly IReadOnlyDictionary<ContentId, string> _packNames;
    private readonly int _totalCount;

    private ContentListState _state = new();
    private readonly RelayCommand _clearFiltersCommand;
    private bool _anyFilterNarrows;
    private string _countText = string.Empty;
    private string _countShownPart = string.Empty;
    private string _countOfPart = string.Empty;
    private string _countTotalPart = string.Empty;
    private string _countNounPart = string.Empty;
    private ContentDetailViewModel? _detail;
    private bool _showSelectionPrompt;
    private bool _hasNoMatches;

    public ContentTabViewModel(
        ContentRegistry registry,
        ContentTabDefinition tab,
        IReadOnlyCollection<ContentTypeReference> allKnownTypes,
        IContentPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentNullException.ThrowIfNull(allKnownTypes);
        ArgumentNullException.ThrowIfNull(presentation);

        Title = tab.Title;
        _presentation = presentation;
        _profilesByType = tab.ContentTypes.ToDictionary(profile => profile.Type);
        _packNames = registry.Packs.ToDictionary(pack => pack.Id, pack => pack.Name);
        _model = new ContentListModel(registry, tab, allKnownTypes);
        EmptyText = tab.EmptyText ?? "Żadna paczka nie ma jeszcze wpisów tego rodzaju.";

        var firstBuild = _model.Build(_state);

        // The tab's own total: loaded and broken rows under every pack, with no filter or search -
        // a rejected pack's row is not an entry and never counts.
        _totalCount = CountShown(firstBuild.Sections);

        CategoryFilter = firstBuild.Category is { } category
            ? new ContentFilterChipViewModel(category.Label, category.Options, values => Apply(_state with { Categories = values }))
            : null;
        PackFilter = new ContentFilterChipViewModel(
            firstBuild.Pack.Label, firstBuild.Pack.Options, values => Apply(_state with { Packs = values }));
        ValueFilters = firstBuild.ValueFilters
            .Select(filter => new ContentFilterChipViewModel(
                filter.Label, filter.Options, values => ApplyValueFilter(filter.Label, values)))
            .ToArray();
        Filters = [.. CategoryFilter is { } categoryChip ? [categoryChip] : Array.Empty<ContentFilterChipViewModel>(), .. ValueFilters, PackFilter];
        SortOptions = firstBuild.Sorts;
        NumericSortOptions = firstBuild.NumericSorts;

        _clearFiltersCommand = new RelayCommand(ClearFilters, () => _anyFilterNarrows);

        ApplyResult(firstBuild);
    }

    /// <summary>The tab's own title - "Potwory", "Przedmioty".</summary>
    public string Title { get; }

    /// <summary>The system's sentence for a tab no pack fills yet (<see cref="ContentTabDefinition.EmptyText"/>).</summary>
    public string EmptyText { get; }

    /// <summary>No pack holds a single loaded or broken entry of this tab's types.</summary>
    public bool HasNoEntries => _totalCount == 0;

    public string Search
    {
        get => _state.Search;
        set => Apply(_state with { Search = value });
    }

    /// <summary>The category chip, named by the system - null when no content type in the tab declares a category.</summary>
    public ContentFilterChipViewModel? CategoryFilter { get; }

    /// <summary>The "Paczka" chip.</summary>
    public ContentFilterChipViewModel PackFilter { get; }

    /// <summary>The system's own value filter chips, in the order the system declares them.</summary>
    public IReadOnlyList<ContentFilterChipViewModel> ValueFilters { get; }

    /// <summary>Every chip in the order the row shows them: the category, the system's filters, "Paczka".</summary>
    public IReadOnlyList<ContentFilterChipViewModel> Filters { get; }

    public IReadOnlyList<string> SortOptions { get; }

    /// <summary>The sorts among <see cref="SortOptions"/> with a numeric or ranked key ("rosnąco" / "malejąco").</summary>
    public IReadOnlyList<string> NumericSortOptions { get; }

    public string SelectedSort
    {
        get => _state.Sort;
        set => Apply(_state with { Sort = value });
    }

    /// <summary>The sort's direction; "Wyczyść filtry" leaves it as it is.</summary>
    public bool SortDescending
    {
        get => _state.SortDescending;
        set => Apply(_state with { SortDescending = value });
    }

    /// <summary>
    /// "Wyczyść filtry": executable only while the search box holds a character or a chip holds a
    /// value - otherwise there is nothing to clear and the link greys out. The same command backs the
    /// "Nic nie pasuje…" state's link, which only shows while something narrows the list.
    /// </summary>
    public System.Windows.Input.ICommand ClearFiltersCommand => _clearFiltersCommand;

    public IReadOnlyList<ContentSectionViewModel> Sections { get; private set; } = [];

    /// <summary>The whole counter as one sentence - "3 wpisy", "1 z 3 wpisy".</summary>
    public string CountText
    {
        get => _countText;
        private set => SetField(ref _countText, value);
    }

    // The counter's parts in reading order, so the view can set numbers in the numeral face and
    // words in the interface face: CountShownPart + CountOfPart + CountTotalPart + CountNounPart
    // is exactly CountText. The first two are empty when nothing is hidden.

    /// <summary>Rows shown, when a filter or search hides some - empty otherwise.</summary>
    public string CountShownPart
    {
        get => _countShownPart;
        private set => SetField(ref _countShownPart, value);
    }

    /// <summary>" z " between the shown and total numbers, when a filter or search hides some - empty otherwise.</summary>
    public string CountOfPart
    {
        get => _countOfPart;
        private set => SetField(ref _countOfPart, value);
    }

    /// <summary>The tab's own total.</summary>
    public string CountTotalPart
    {
        get => _countTotalPart;
        private set => SetField(ref _countTotalPart, value);
    }

    /// <summary>" wpis", " wpisy" or " wpisów", agreeing with the total.</summary>
    public string CountNounPart
    {
        get => _countNounPart;
        private set => SetField(ref _countNounPart, value);
    }

    /// <summary>The detail column's content - null when nothing is selected.</summary>
    public ContentDetailViewModel? Detail
    {
        get => _detail;
        private set
        {
            if (SetField(ref _detail, value))
            {
                RaisePropertyChanged(nameof(HasDetail));
            }
        }
    }

    public bool HasDetail => Detail is not null;

    /// <summary>Invites a selection - never true at the same time as <see cref="Detail"/> is non-null.</summary>
    public bool ShowSelectionPrompt
    {
        get => _showSelectionPrompt;
        private set => SetField(ref _showSelectionPrompt, value);
    }

    /// <summary>
    /// The tab holds entries, but today's filters and search show none of them - the list says so and
    /// points at clearing the filters. False for a tab with no entries at all: there is nothing to
    /// clear.
    /// </summary>
    public bool HasNoMatches
    {
        get => _hasNoMatches;
        private set => SetField(ref _hasNoMatches, value);
    }

    private void ApplyValueFilter(string label, IReadOnlyCollection<string> values)
    {
        var current = _state.ValueFilters is { } existing
            ? new Dictionary<string, IReadOnlyCollection<string>>(existing, StringComparer.Ordinal)
            : new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        if (values.Count == 0)
        {
            current.Remove(label);
        }
        else
        {
            current[label] = values;
        }

        Apply(_state with { ValueFilters = current.Count == 0 ? null : current });
    }

    // "Wyczyść filtry": every chip and the search box. The search box is bound two-way, so it has
    // to hear that its text changed from here.
    private void ClearFilters()
    {
        Apply(_state.ClearFilters());
        RaisePropertyChanged(nameof(Search));
    }

    private void Select(ContentSelectionKey key) => Apply(_state with { Selected = key });

    private void Apply(ContentListState next)
    {
        _state = next;
        ApplyResult(_model.Build(_state));
    }

    private void ApplyResult(ContentListResult result)
    {
        CategoryFilter?.SyncFromResult(result.Category?.Options ?? [], _state.Categories);
        PackFilter.SyncFromResult(result.Pack.Options, _state.Packs);

        foreach (var filter in ValueFilters)
        {
            var options = result.ValueFilters.FirstOrDefault(f => f.Label == filter.Label)?.Options ?? [];
            var selected = _state.ValueFilters is { } selections && selections.TryGetValue(filter.Label, out var values)
                ? values
                : null;

            filter.SyncFromResult(options, selected);
        }

        // A selected row that today's filters or search no longer show drops the selection for good:
        // widening them again does not bring it back.
        if (_state.Selected is not null && result.Selection is null)
        {
            _state = _state with { Selected = null };
        }

        Sections = result.Sections.Select(BuildSection).ToArray();
        RaisePropertyChanged(nameof(Sections));

        var shown = CountShown(result.Sections);
        SetCount(shown, _totalCount);

        var anyFilterNarrows = _state.AnyFilterNarrows;
        if (anyFilterNarrows != _anyFilterNarrows)
        {
            _anyFilterNarrows = anyFilterNarrows;
            _clearFiltersCommand.RaiseCanExecuteChanged();
        }
        Detail = BuildDetail(result.Selection);

        // Inviting a pick only makes sense when something is currently on screen to pick.
        ShowSelectionPrompt = Detail is null && Sections.Count > 0;
        HasNoMatches = shown == 0 && _totalCount > 0;
    }

    private static int CountShown(IEnumerable<ContentSection> sections) =>
        sections.Where(section => !section.IsRejectedPack).Sum(section => section.ShownCount);

    private ContentSectionViewModel BuildSection(ContentSection section)
    {
        if (section.IsRejectedPack)
        {
            var pack = section.RejectedPack!;
            var row = BuildRow(pack.Location, badgeText: null, badgeBrush: null, isBroken: true, new RejectedPackSelectionKey(pack.Location));

            return new ContentSectionViewModel(section.Header, 0, isRejectedPack: true, [row]);
        }

        var validRows = section.ValidRows.Select(BuildValidRow);
        var brokenRows = section.BrokenRows.Select(BuildBrokenRow);

        return new ContentSectionViewModel(section.Header, section.ShownCount, isRejectedPack: false, [.. validRows, .. brokenRows]);
    }

    private ContentRowViewModel BuildValidRow(RegisteredEntry entry)
    {
        var profile = _profilesByType[entry.Type!.Value.Reference];
        var badge = profile.Badge(entry.Entry);
        var badgeText = badge.Text is { Length: > 0 } text ? text : null;
        var badgeBrush = badge.ColorKey is { } colorKey ? _presentation.ResolveBadgeBrush(colorKey) : null;

        return BuildRow(entry.Entry.Name, badgeText, badgeBrush, isBroken: false, new EntrySelectionKey(entry.Address));
    }

    private ContentRowViewModel BuildBrokenRow(ContentBrokenRow row)
    {
        var key = row.UnresolvedEntry is { } unresolved
            ? (ContentSelectionKey)new EntrySelectionKey(unresolved.Address)
            : new RejectedFileSelectionKey(row.RejectedFile!.Pack, row.RejectedFile!.Location);

        return BuildRow(row.DisplayName, badgeText: null, badgeBrush: null, isBroken: true, key);
    }

    private ContentRowViewModel BuildRow(
        string name, string? badgeText, Avalonia.Media.IBrush? badgeBrush, bool isBroken, ContentSelectionKey key)
    {
        var row = new ContentRowViewModel(name, badgeText, badgeBrush, isBroken, key, Select);
        row.IsSelected = _state.Selected == key;

        return row;
    }

    private ContentDetailViewModel? BuildDetail(ContentSelectionDetail? selection) => selection switch
    {
        ValidEntrySelection valid => new ValidContentDetailViewModel(
            valid.Category is { } category ? [Title, category, valid.Name] : [Title, valid.Name],
            valid.Name,
            valid.Tags,
            _presentation.CreateCard(valid.Entry.Entry)),

        BrokenRowSelection broken => BuildBrokenDetail(broken.Row),

        RejectedPackSelection rejected => new BrokenContentDetailViewModel(
            "Paczka odrzucona",
            rejected.Pack.Location,
            $"Nie udało się wczytać tej paczki: {rejected.Pack.Reason}",
            rejected.Pack.Location),

        _ => null,
    };

    private BrokenContentDetailViewModel BuildBrokenDetail(ContentBrokenRow row)
    {
        var overline = $"Wpis niewczytany · {PackName(row.Pack)}";

        if (row.UnresolvedEntry is { } entry)
        {
            return new BrokenContentDetailViewModel(overline, entry.Entry.Name, DescribeUnresolved(entry), path: null);
        }

        var file = row.RejectedFile!;
        return new BrokenContentDetailViewModel(
            overline, file.Location, $"Nie udało się wczytać tego pliku: {file.Reason}", file.Location);
    }

    private string PackName(ContentId pack) => _packNames.TryGetValue(pack, out var name) ? name : pack.ToString();

    private static string DescribeUnresolved(RegisteredEntry entry)
    {
        var setId = entry.Entry.Type.Set.ToString();
        var typeId = entry.Entry.Type.Type.ToString();

        return entry.Unresolved switch
        {
            EntryUnresolvedReason.MissingSet =>
                $"Ten wpis odwołuje się do systemu „{setId}”, który nie jest zainstalowany.",
            EntryUnresolvedReason.MissingType =>
                $"System „{setId}” nie zna już typu treści „{typeId}”.",
            EntryUnresolvedReason.TypeVersionMismatch =>
                $"Typ treści „{typeId}” w systemie „{setId}” zmienił się od czasu zapisania tego wpisu. Trzeba go zapisać ponownie.",
            EntryUnresolvedReason.ValuesRejected =>
                $"System „{setId}” nie przyjął wartości tego wpisu dla typu „{typeId}”: {entry.UnresolvedDetail}",
            _ => "Tego wpisu nie da się wyświetlić.",
        };
    }

    /// <summary>
    /// Polish plural agreement for "wpis" against a count: "1 wpis", "2 wpisy", "5 wpisów", "12
    /// wpisów", "22 wpisy". "<paramref name="shown"/> z <paramref name="total"/> wpisów" when a
    /// filter hides something - agreeing with <paramref name="total"/>, the second number. Sets the
    /// parts and the whole sentence together, the whole being the parts joined.
    /// </summary>
    private void SetCount(int shown, int total)
    {
        var filtered = shown != total;
        CountShownPart = filtered ? $"{shown}" : string.Empty;
        CountOfPart = filtered ? " z " : string.Empty;
        CountTotalPart = $"{total}";
        CountNounPart = $" {Plural(total)}";
        CountText = CountShownPart + CountOfPart + CountTotalPart + CountNounPart;
    }

    private static string Plural(int count)
    {
        if (count == 1)
        {
            return "wpis";
        }

        var lastDigit = count % 10;
        var lastTwoDigits = count % 100;

        return lastDigit is >= 2 and <= 4 && lastTwoDigits is < 12 or > 14 ? "wpisy" : "wpisów";
    }
}
