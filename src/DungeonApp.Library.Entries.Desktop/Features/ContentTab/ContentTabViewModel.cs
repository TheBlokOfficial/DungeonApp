using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.ViewModels;
using DungeonApp.Library.Entries.Desktop.Content;

namespace DungeonApp.Library.Entries.Desktop.Features.ContentTab;

/// <summary>
/// The view model behind one content tab's whole screen (docs/architecture.md, "Zakładki treści"):
/// wraps one <see cref="ContentListModel"/> - built once, for this tab's own
/// <see cref="ContentTabDefinition"/> - and a mutable <see cref="ContentListState"/>, and exposes
/// exactly what the skeleton view draws. Every mutation (search, a filter, a sort, a selection,
/// "Wyczyść filtry") builds the next immutable state and passes it through <c>Apply</c>, the only
/// place <see cref="ContentListModel.Build"/> is ever called - the same discipline
/// docs/architecture.md's "Stanu nie zmienia się w miejscu" describes for campaign state, at
/// list-state scale.
/// </summary>
public sealed class ContentTabViewModel : ObservableObject
{
    private readonly ContentListModel _model;
    private readonly IContentPresentation _presentation;
    private readonly IReadOnlyDictionary<ContentTypeReference, IContentTypeProfile> _profilesByType;
    private readonly IReadOnlyDictionary<ContentId, string> _packNames;
    private readonly int _totalCount;

    private ContentListState _state = new();
    private string _countText = string.Empty;
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
        // a rejected pack's row is not an entry and never counts (krok 10, brief 3a).
        _totalCount = CountShown(firstBuild.Sections);

        CategoryFilter = new ContentFilterChipViewModel(
            "Kategoria", firstBuild.Category.Options, value => Apply(_state with { Category = value }));
        SourceFilter = new ContentFilterChipViewModel(
            "Źródło", firstBuild.Source.Options, value => Apply(_state with { Source = value }));
        ValueFilters = firstBuild.ValueFilters
            .Select(filter => new ContentFilterChipViewModel(
                filter.Label, filter.Options, value => ApplyValueFilter(filter.Label, value)))
            .ToArray();
        SortOptions = firstBuild.Sorts;

        ClearFiltersCommand = new RelayCommand(() => Apply(_state.ClearFilters()));

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

    public ContentFilterChipViewModel CategoryFilter { get; }

    public ContentFilterChipViewModel SourceFilter { get; }

    public IReadOnlyList<ContentFilterChipViewModel> ValueFilters { get; }

    public IReadOnlyList<string> SortOptions { get; }

    public string SelectedSort
    {
        get => _state.Sort;
        set => Apply(_state with { Sort = value });
    }

    public System.Windows.Input.ICommand ClearFiltersCommand { get; }

    public IReadOnlyList<ContentSectionViewModel> Sections { get; private set; } = [];

    public string CountText
    {
        get => _countText;
        private set => SetField(ref _countText, value);
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

    private void ApplyValueFilter(string label, string? value)
    {
        var current = _state.ValueFilters is { } existing
            ? new Dictionary<string, string>(existing, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        if (value is null)
        {
            current.Remove(label);
        }
        else
        {
            current[label] = value;
        }

        Apply(_state with { ValueFilters = current.Count == 0 ? null : current });
    }

    private void Select(ContentSelectionKey key) => Apply(_state with { Selected = key });

    private void Apply(ContentListState next)
    {
        _state = next;
        ApplyResult(_model.Build(_state));
    }

    private void ApplyResult(ContentListResult result)
    {
        CategoryFilter.SyncFromResult(result.Category.Options, _state.Category);
        SourceFilter.SyncFromResult(result.Source.Options, _state.Source);

        foreach (var filter in ValueFilters)
        {
            var options = result.ValueFilters.FirstOrDefault(f => f.Label == filter.Label)?.Options ?? [];
            var selected = _state.ValueFilters is { } selections && selections.TryGetValue(filter.Label, out var value)
                ? value
                : null;

            filter.SyncFromResult(options, selected);
        }

        // A selected row that today's filters or search no longer show drops the selection for good:
        // widening them again does not bring it back (krok 10, brief 3a).
        if (_state.Selected is not null && result.Selection is null)
        {
            _state = _state with { Selected = null };
        }

        Sections = result.Sections.Select((section, index) => BuildSection(section, index == 0)).ToArray();
        RaisePropertyChanged(nameof(Sections));

        var shown = CountShown(result.Sections);
        CountText = BuildCountText(shown, _totalCount);
        Detail = BuildDetail(result.Selection);

        // Inviting a pick only makes sense when something is currently on screen to pick.
        ShowSelectionPrompt = Detail is null && Sections.Count > 0;
        HasNoMatches = shown == 0 && _totalCount > 0;
    }

    private static int CountShown(IEnumerable<ContentSection> sections) =>
        sections.Where(section => !section.IsRejectedPack).Sum(section => section.ShownCount);

    private ContentSectionViewModel BuildSection(ContentSection section, bool isFirst)
    {
        if (section.IsRejectedPack)
        {
            var pack = section.RejectedPack!;
            var row = BuildRow(pack.Location, badgeText: null, badgeBrush: null, isBroken: true, new RejectedPackSelectionKey(pack.Location));

            return new ContentSectionViewModel(section.Header, 0, isRejectedPack: true, [row]) { IsFirst = isFirst };
        }

        var validRows = section.ValidRows.Select(BuildValidRow);
        var brokenRows = section.BrokenRows.Select(BuildBrokenRow);

        return new ContentSectionViewModel(section.Header, section.ShownCount, isRejectedPack: false, [.. validRows, .. brokenRows])
        {
            IsFirst = isFirst,
        };
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
    /// filter hides something - agreeing with <paramref name="total"/>, the second number, exactly
    /// as docs/architecture.md's "Co ma być na ekranie" describes.
    /// </summary>
    private static string BuildCountText(int shown, int total) =>
        shown == total ? $"{total} {Plural(total)}" : $"{shown} z {total} {Plural(total)}";

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
