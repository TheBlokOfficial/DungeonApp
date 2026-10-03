using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Tests.Entries;

/// <summary>
/// <see cref="ContentTabViewModel"/> against a fake "widget" content type - the same discipline
/// <c>ContentListModelTests</c> follows. This project never names a real content type, so a
/// hand-built <see cref="ContentRegistry"/> stands in for <see cref="ContentPackLoader"/>'s output
/// - <c>ContentListModel</c> itself already has exhaustive coverage of section/filter/sort rules
/// (<c>ContentListModelTests</c>); these tests only check that the view model drives it correctly
/// and exposes the right bindable shape.
/// </summary>
public sealed class ContentTabViewModelTests
{
    private static readonly ContentId TestSet = ContentId.Create("test");
    private static readonly ContentTypeReference WidgetType = new(TestSet, ContentId.Create("widget"));

    private sealed record Widget(string Tier);

    private sealed class FakePresentation : IContentPresentation
    {
        public IBrush? Brush { get; set; }

        public Control CreateCard(Entry entry, EntryPicture picture) => new TextBlock { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => Brush;
    }

    private static readonly IComparer<string> TierOrder = Comparer<string>.Create(string.CompareOrdinal);

    private static IContentTypeProfile WidgetProfile() => new ContentTypeProfile<Widget>(
        WidgetType,
        tags: widget => [widget.Tier],
        badge: widget => new ContentBadge(widget.Tier, "tier-key"),
        valueFilters: [new ContentValueFilterSpec<Widget>("Poziom", widget => widget.Tier, TierOrder)],
        sorts: [new ContentSortSpec<Widget>("Poziom", (a, b) => TierOrder.Compare(a.Tier, b.Tier))]);

    private static ContentTabDefinition WidgetTab() => new("Widgety", [WidgetProfile()]);

    private static readonly ContentTypeReference[] AllKnownTypes = [WidgetType];

    private static Entry MakeEntry(string id, string name, string tier) =>
        new(ContentId.Create(id), name, WidgetType, TypeVersion: 1, ContentValues.From(new Widget(tier)));

    private static RegisteredEntry Valid(string pack, string id, string name, string tier) =>
        RegisteredEntry.CreateResolved(
            new EntryAddress(ContentId.Create(pack), ContentId.Create(id)),
            MakeEntry(id, name, tier),
            new ContentTypeDescriptor(WidgetType, "Widget", 1));

    private static RegisteredEntry Broken(string pack, string id, string name, EntryUnresolvedReason reason, string? detail = null) =>
        RegisteredEntry.CreateUnresolved(
            new EntryAddress(ContentId.Create(pack), ContentId.Create(id)),
            MakeEntry(id, name, "1"),
            reason,
            detail);

    private static Pack MakePack(string id, string name, params Entry[] entries) =>
        new(ContentId.Create(id), name, new PackVersion(1, 0), entries);

    private static ContentTabViewModel BuildViewModel(
        ContentRegistry registry, FakePresentation? presentation = null) =>
        new(registry, WidgetTab(), AllKnownTypes, presentation ?? new FakePresentation());

    private static IReadOnlyList<string> AllRowNames(ContentTabViewModel viewModel) =>
        [.. viewModel.Sections.SelectMany(section => section.Rows).Select(row => row.Name)];

    // -----------------------------------------------------------------------------------------
    // Search / filter / sort rebuild the list.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Changing_search_rebuilds_the_shown_rows_and_count()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "1"), MakeEntry("b", "Beta", "1"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "1"), Valid("p", "b", "Beta", "1")], [], []);
        var viewModel = BuildViewModel(registry);

        Assert.Equal(["Alpha", "Beta"], AllRowNames(viewModel));
        Assert.Equal("2 wpisy", viewModel.CountText);

        viewModel.Search = "Alp";

        Assert.Equal(["Alpha"], AllRowNames(viewModel));
        Assert.Equal("1 z 2 wpisy", viewModel.CountText);
    }

    [Fact]
    public void A_search_that_matches_nothing_reports_no_matches_and_notifies_both_ways()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "1"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "1")], [], []);
        var viewModel = BuildViewModel(registry);
        var notified = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notified.Add(args.PropertyName);

        Assert.False(viewModel.HasNoMatches);

        viewModel.Search = "Zzz";

        Assert.True(viewModel.HasNoMatches);
        Assert.Equal(1, notified.Count(name => name == nameof(ContentTabViewModel.HasNoMatches)));

        viewModel.Search = string.Empty;

        Assert.False(viewModel.HasNoMatches);
        Assert.Equal(2, notified.Count(name => name == nameof(ContentTabViewModel.HasNoMatches)));
    }

    [Fact]
    public void A_tab_without_entries_is_not_a_filter_with_no_matches()
    {
        var registry = new ContentRegistry([MakePack("p", "Pack")], [], [], []);
        var viewModel = BuildViewModel(registry);

        Assert.False(viewModel.HasNoMatches);
    }

    [Fact]
    public void Changing_a_value_filter_rebuilds_the_list()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "high"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "high")], [], []);
        var viewModel = BuildViewModel(registry);

        var levelFilter = Assert.Single(viewModel.ValueFilters, f => f.Label == "Poziom");
        levelFilter.SelectedValues.Add("high");

        Assert.Equal(["Beta"], AllRowNames(viewModel));
        Assert.True(levelFilter.IsActive);
    }

    [Fact]
    public void Two_values_in_one_chip_show_both_and_unchecking_them_all_shows_everything()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "high"), MakeEntry("c", "Gamma", "mid"));
        var registry = new ContentRegistry(
            [pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "high"), Valid("p", "c", "Gamma", "mid")], [], []);
        var viewModel = BuildViewModel(registry);
        var levelFilter = Assert.Single(viewModel.ValueFilters, f => f.Label == "Poziom");

        levelFilter.SelectedValues.Add("low");
        levelFilter.SelectedValues.Add("high");

        Assert.Equal(["Alpha", "Beta"], AllRowNames(viewModel));
        Assert.Equal(["high", "low", "mid"], levelFilter.Options);

        levelFilter.SelectedValues.Clear();

        Assert.Equal(["Alpha", "Beta", "Gamma"], AllRowNames(viewModel));
        Assert.False(viewModel.ClearFiltersCommand.CanExecute(null));
    }

    [Fact]
    public void Chips_run_category_then_system_filters_then_paczka_and_a_tab_without_a_category_has_no_category_chip()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low")], [], []);
        var viewModel = BuildViewModel(registry);

        Assert.Null(viewModel.CategoryFilter);
        Assert.Equal(["Poziom", "Paczka"], viewModel.Filters.Select(filter => filter.Label));
    }

    [Fact]
    public void Changing_sort_reorders_rows()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "z"), MakeEntry("b", "Beta", "a"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "z"), Valid("p", "b", "Beta", "a")], [], []);
        var viewModel = BuildViewModel(registry);

        Assert.Equal(["Alpha", "Beta"], AllRowNames(viewModel));

        viewModel.SelectedSort = "Poziom";

        Assert.Equal(["Beta", "Alpha"], AllRowNames(viewModel));
    }

    [Fact]
    public void Wyczysc_filtry_resets_search_and_filters_but_not_sort()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "high"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "high")], [], []);
        var viewModel = BuildViewModel(registry);

        viewModel.Search = "Alp";
        viewModel.SelectedSort = "Poziom";
        var levelFilter = Assert.Single(viewModel.ValueFilters, f => f.Label == "Poziom");
        levelFilter.SelectedValues.Add("low");

        viewModel.ClearFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Search);
        Assert.False(levelFilter.IsActive);
        Assert.Empty(levelFilter.SelectedValues);
        Assert.Equal("Poziom", viewModel.SelectedSort);
        Assert.Equal(["Beta", "Alpha"], AllRowNames(viewModel));
    }

    [Fact]
    public void Wyczysc_filtry_is_executable_only_while_something_narrows_the_list()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "high"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "high")], [], []);
        var viewModel = BuildViewModel(registry);
        var command = viewModel.ClearFiltersCommand;
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        Assert.False(command.CanExecute(null));

        viewModel.Search = "A";

        Assert.True(command.CanExecute(null));
        Assert.Equal(1, raised);

        viewModel.Search = string.Empty;

        Assert.False(command.CanExecute(null));
        Assert.Equal(2, raised);

        var levelFilter = Assert.Single(viewModel.ValueFilters, f => f.Label == "Poziom");
        levelFilter.SelectedValues.Add("low");

        Assert.True(command.CanExecute(null));
        Assert.Equal(3, raised);

        command.Execute(null);

        Assert.False(command.CanExecute(null));
        Assert.Equal(4, raised);
    }

    // -----------------------------------------------------------------------------------------
    // Selection and detail.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Selecting_a_valid_entry_builds_breadcrumbs_and_a_card()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low")], [], []);
        var viewModel = BuildViewModel(registry);

        var row = Assert.Single(AllRows(viewModel));
        row.SelectCommand.Execute(null);

        var detail = Assert.IsType<ValidContentDetailViewModel>(viewModel.Detail);
        Assert.Equal("Alpha", detail.Name);
        Assert.Equal(["Widgety", "Alpha"], detail.Breadcrumbs);
        Assert.NotNull(detail.Card);
        Assert.False(viewModel.ShowSelectionPrompt);
    }

    [Fact]
    public void Selecting_a_broken_row_gives_its_reason()
    {
        var pack = MakePack("p", "Pack");
        var registry = new ContentRegistry(
            [pack], [Broken("p", "z", "Ghost", EntryUnresolvedReason.MissingSet)], [], []);
        var viewModel = BuildViewModel(registry);

        var brokenRow = viewModel.Sections.Single().Rows.Single(r => r.IsBroken);
        brokenRow.SelectCommand.Execute(null);

        var detail = Assert.IsType<BrokenContentDetailViewModel>(viewModel.Detail);
        Assert.Equal("Ghost", detail.Name);
        Assert.Contains("nie jest zainstalowany", detail.Reason);
    }

    [Fact]
    public void Selecting_a_rejected_packs_header_gives_its_reason()
    {
        var registry = new ContentRegistry([], [], [], [new RejectedPack("bad-dir", "manifest is unreadable.")]);
        var viewModel = BuildViewModel(registry);

        var section = Assert.Single(viewModel.Sections, s => s.IsRejectedPack);
        var packRow = Assert.Single(section.Rows);
        packRow.SelectCommand.Execute(null);

        var detail = Assert.IsType<BrokenContentDetailViewModel>(viewModel.Detail);
        Assert.Equal("bad-dir", detail.Name);
        Assert.Contains("manifest is unreadable.", detail.Reason);
    }

    [Fact]
    public void A_recognised_color_key_resolves_to_a_brush_on_the_row()
    {
        var brush = new SolidColorBrush(Colors.Red);
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low")], [], []);
        var viewModel = BuildViewModel(registry, new FakePresentation { Brush = brush });

        var row = Assert.Single(AllRows(viewModel));

        Assert.Same(brush, row.BadgeBrush);
        Assert.True(row.HasColoredBadge);
        Assert.False(row.HasPlainBadge);
    }

    private static IReadOnlyList<ContentRowViewModel> AllRows(ContentTabViewModel viewModel) =>
        [.. viewModel.Sections.SelectMany(section => section.Rows)];

    // -----------------------------------------------------------------------------------------
    // Polish plural agreement.
    // -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, "1 wpis")]
    [InlineData(2, "2 wpisy")]
    [InlineData(5, "5 wpisów")]
    [InlineData(4, "4 wpisy")]
    [InlineData(12, "12 wpisów")]
    [InlineData(14, "14 wpisów")]
    [InlineData(21, "21 wpisów")]
    [InlineData(22, "22 wpisy")]
    [InlineData(24, "24 wpisy")]
    [InlineData(25, "25 wpisów")]
    public void Count_text_uses_polish_plural_agreement(int count, string expected)
    {
        var entries = Enumerable.Range(0, count).Select(i => MakeEntry($"e{i}", $"Name{i}", "1")).ToArray();
        var pack = MakePack("p", "Pack", entries);
        var registeredEntries = Enumerable.Range(0, count).Select(i => Valid("p", $"e{i}", $"Name{i}", "1")).ToArray();
        var registry = new ContentRegistry([pack], registeredEntries, [], []);
        var viewModel = BuildViewModel(registry);

        Assert.Equal(expected, viewModel.CountText);
    }

    [Fact]
    public void Count_counts_broken_rows_but_not_a_rejected_packs_row()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "1"), MakeEntry("b", "Beta", "1"));
        var registry = new ContentRegistry(
            [pack],
            [Valid("p", "a", "Alpha", "1"), Valid("p", "b", "Beta", "1"), Broken("p", "z", "Ghost", EntryUnresolvedReason.MissingSet)],
            [],
            [new RejectedPack("bad-dir", "manifest is unreadable.")]);
        var viewModel = BuildViewModel(registry);

        Assert.Equal("3 wpisy", viewModel.CountText);

        viewModel.Search = "Alp";

        Assert.Equal("1 z 3 wpisy", viewModel.CountText);
    }

    // -----------------------------------------------------------------------------------------
    // Selection across filter changes.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Nothing_is_selected_when_the_tab_opens()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low")], [], []);
        var viewModel = BuildViewModel(registry);

        Assert.Null(viewModel.Detail);
        Assert.Equal([false], AllRows(viewModel).Select(row => row.IsSelected));
        Assert.True(viewModel.ShowSelectionPrompt);
    }

    [Fact]
    public void A_selected_entry_hidden_by_search_loses_the_selection_for_good()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "low"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "low")], [], []);
        var viewModel = BuildViewModel(registry);

        AllRows(viewModel).Single(row => row.Name == "Alpha").SelectCommand.Execute(null);
        viewModel.Search = "Bet";

        Assert.Null(viewModel.Detail);

        viewModel.Search = string.Empty;

        Assert.Null(viewModel.Detail);
        Assert.Equal([false, false], AllRows(viewModel).Select(row => row.IsSelected));
    }

    [Fact]
    public void A_selected_entry_still_shown_after_a_filter_stays_selected()
    {
        var pack = MakePack("p", "Pack", MakeEntry("a", "Alpha", "low"), MakeEntry("b", "Beta", "high"));
        var registry = new ContentRegistry([pack], [Valid("p", "a", "Alpha", "low"), Valid("p", "b", "Beta", "high")], [], []);
        var viewModel = BuildViewModel(registry);

        AllRows(viewModel).Single(row => row.Name == "Alpha").SelectCommand.Execute(null);
        viewModel.Search = "Al";

        var detail = Assert.IsType<ValidContentDetailViewModel>(viewModel.Detail);
        Assert.Equal("Alpha", detail.Name);
        Assert.Equal([true], AllRows(viewModel).Select(row => row.IsSelected));
    }
}
