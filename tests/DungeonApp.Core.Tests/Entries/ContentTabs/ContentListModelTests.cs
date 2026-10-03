using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries;

namespace DungeonApp.Core.Tests.Entries.ContentTabs;

/// <summary>
/// One test per list rule, on a fake pair of content types ("widget"/"gadget") so this project
/// never names a real one - the same discipline <c>InstanceResolverTests</c> and
/// <c>ContentPackLoaderTests</c> follow.
/// </summary>
public sealed class ContentListModelTests
{
    private static readonly ContentId TestSet = ContentId.Create("test");
    private static readonly ContentTypeReference WidgetType = new(TestSet, ContentId.Create("widget"));
    private static readonly ContentTypeReference GadgetType = new(TestSet, ContentId.Create("gadget"));
    private static readonly ContentTypeReference GhostType = new(TestSet, ContentId.Create("ghost"));

    private sealed record Widget(string Kind, string Tier);

    private static readonly IComparer<string> TierOrder =
        Comparer<string>.Create((x, y) => string.CompareOrdinal(x, y));

    private static IContentTypeProfile WidgetProfile() => new ContentTypeProfile<Widget>(
        WidgetType,
        category: new ContentCategorySpec<Widget>("Rodzaj", widget => widget.Kind),
        tags: widget => [widget.Tier],
        badge: widget => new ContentBadge(widget.Tier, $"tier-{widget.Tier}"),
        valueFilters: [new ContentValueFilterSpec<Widget>("Poziom", widget => widget.Tier, TierOrder)],
        sorts: [new ContentSortSpec<Widget>("Poziom", (a, b) => TierOrder.Compare(a.Tier, b.Tier))]);

    private static IContentTypeProfile GadgetProfile() => new ContentTypeProfile<object>(GadgetType);

    private static ContentTabDefinition WidgetTab() => new("Widgety", [WidgetProfile()]);

    private static readonly ContentTypeReference[] AllKnownTypes = [WidgetType, GadgetType];

    private static Entry MakeEntry(string id, string name, ContentTypeReference type, object values) =>
        new(ContentId.Create(id), name, type, TypeVersion: 1, ContentValues.From(values));

    private static RegisteredEntry Valid(string pack, string id, string name, ContentTypeReference type, string kind, string tier) =>
        RegisteredEntry.CreateResolved(
            new EntryAddress(ContentId.Create(pack), ContentId.Create(id)),
            MakeEntry(id, name, type, new Widget(kind, tier)),
            new ContentTypeDescriptor(type, "Widget", 1));

    private static RegisteredEntry Broken(string pack, string id, string name, ContentTypeReference type, EntryUnresolvedReason reason) =>
        RegisteredEntry.CreateUnresolved(
            new EntryAddress(ContentId.Create(pack), ContentId.Create(id)),
            MakeEntry(id, name, type, new Widget("x", "1")),
            reason,
            detail: reason == EntryUnresolvedReason.ValuesRejected ? "rejected." : null);

    private static Pack MakePack(string id, string name, params Entry[] entries) =>
        new(ContentId.Create(id), name, new PackVersion(1, 0), entries);

    private static ContentListModel ModelFor(ContentTabDefinition tab, ContentRegistry registry) =>
        new(registry, tab, AllKnownTypes);

    // -----------------------------------------------------------------------------------------
    // Sections: order, disappearance, contents.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Sections_order_valid_packs_alphabetically_case_insensitively_then_rejected_packs_last()
    {
        var beta = MakePack("beta", "beta pack", MakeEntry("b1", "B", WidgetType, new Widget("k", "1")));
        var alpha = MakePack("alpha", "Alpha Pack", MakeEntry("a1", "A", WidgetType, new Widget("k", "1")));
        var registry = new ContentRegistry(
            [beta, alpha],
            [Valid("beta", "b1", "B", WidgetType, "k", "1"), Valid("alpha", "a1", "A", WidgetType, "k", "1")],
            [],
            [new RejectedPack("zeta-dir", "manifest is unreadable.")]);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        Assert.Equal(["Alpha Pack", "beta pack", "zeta-dir"], result.Sections.Select(section => section.Header));
    }

    [Fact]
    public void A_valid_pack_section_with_nothing_shown_disappears()
    {
        var pack = MakePack("alpha", "Alpha", MakeEntry("a1", "A", WidgetType, new Widget("k", "1")));
        var registry = new ContentRegistry([pack], [Valid("alpha", "a1", "A", WidgetType, "k", "1")], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Search: "does-not-match"));

        Assert.Empty(result.Sections);
    }

    [Fact]
    public void A_rejected_pack_section_never_disappears_and_carries_no_rows()
    {
        var registry = new ContentRegistry([], [], [], [new RejectedPack("broken-dir", "pack.json is not valid JSON.")]);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Search: "does-not-match"));

        var section = Assert.Single(result.Sections);
        Assert.True(section.IsRejectedPack);
        Assert.Equal("broken-dir", section.Header);
        Assert.Equal("pack.json is not valid JSON.", section.RejectedPack!.Reason);
        Assert.Empty(section.ValidRows);
        Assert.Empty(section.BrokenRows);
    }

    [Fact]
    public void Within_a_section_valid_rows_come_before_broken_rows()
    {
        var entries = new[]
        {
            MakeEntry("a1", "Alfa", WidgetType, new Widget("k", "1")),
            MakeEntry("b1", "Beta", WidgetType, new Widget("k", "1")),
        };
        var pack = MakePack("alpha", "Alpha", entries);
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Alfa", WidgetType, "k", "1"),
                Broken("alpha", "b1", "Beta", WidgetType, EntryUnresolvedReason.ValuesRejected),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        var section = Assert.Single(result.Sections);
        Assert.Single(section.ValidRows);
        Assert.Single(section.BrokenRows);
    }

    [Fact]
    public void Broken_rows_sort_by_name_then_by_file_location_when_there_is_no_name()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [Broken("alpha", "z1", "Zeta", WidgetType, EntryUnresolvedReason.ValuesRejected)],
            [new RejectedEntry(ContentId.Create("alpha"), "entries/aardvark.json", "is not valid JSON.")],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        // pl-PL, case insensitive, like every other user-visible order in this model.
        var section = Assert.Single(result.Sections);
        Assert.Equal(["entries/aardvark.json", "Zeta"], section.BrokenRows.Select(row => row.DisplayName));
    }

    // -----------------------------------------------------------------------------------------
    // Routing a broken entry to the right tab(s).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void A_values_rejected_entry_shows_only_in_its_own_types_tab()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack], [Broken("alpha", "a1", "A", WidgetType, EntryUnresolvedReason.ValuesRejected)], [], []);

        var widgetResult = ModelFor(WidgetTab(), registry).Build(new ContentListState());
        var gadgetResult = ModelFor(new ContentTabDefinition("Gadżety", [GadgetProfile()]), registry).Build(new ContentListState());

        Assert.Single(widgetResult.Sections.Single().BrokenRows);
        Assert.Empty(gadgetResult.Sections);
    }

    [Fact]
    public void A_type_version_mismatch_entry_counts_as_known_and_also_shows_only_in_its_own_tab()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack], [Broken("alpha", "a1", "A", WidgetType, EntryUnresolvedReason.TypeVersionMismatch)], [], []);

        var widgetResult = ModelFor(WidgetTab(), registry).Build(new ContentListState());
        var gadgetResult = ModelFor(new ContentTabDefinition("Gadżety", [GadgetProfile()]), registry).Build(new ContentListState());

        Assert.Single(widgetResult.Sections.Single().BrokenRows);
        Assert.Empty(gadgetResult.Sections);
    }

    [Fact]
    public void A_missing_set_entry_shows_in_every_content_tab()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack], [Broken("alpha", "a1", "A", GhostType, EntryUnresolvedReason.MissingSet)], [], []);

        var widgetResult = ModelFor(WidgetTab(), registry).Build(new ContentListState());
        var gadgetResult = ModelFor(new ContentTabDefinition("Gadżety", [GadgetProfile()]), registry).Build(new ContentListState());

        Assert.Single(widgetResult.Sections.Single().BrokenRows);
        Assert.Single(gadgetResult.Sections.Single().BrokenRows);
    }

    [Fact]
    public void A_rejected_file_shows_in_every_content_tab()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack], [], [new RejectedEntry(ContentId.Create("alpha"), "entries/e.json", "is not valid JSON.")], []);

        var widgetResult = ModelFor(WidgetTab(), registry).Build(new ContentListState());
        var gadgetResult = ModelFor(new ContentTabDefinition("Gadżety", [GadgetProfile()]), registry).Build(new ContentListState());

        Assert.Single(widgetResult.Sections.Single().BrokenRows);
        Assert.Single(gadgetResult.Sections.Single().BrokenRows);
    }

    // -----------------------------------------------------------------------------------------
    // Search.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Search_matches_a_substring_case_insensitively_on_valid_and_broken_names()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Żółw Morski", WidgetType, "k", "1"),
                Broken("alpha", "b1", "Żubr", WidgetType, EntryUnresolvedReason.ValuesRejected),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Search: "żó"));

        var section = Assert.Single(result.Sections);
        Assert.Single(section.ValidRows);
        Assert.Empty(section.BrokenRows);
    }

    [Fact]
    public void Search_does_not_hide_rejected_pack_headers()
    {
        var registry = new ContentRegistry([], [], [], [new RejectedPack("broken-dir", "reason")]);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Search: "nothing-matches-this"));

        Assert.Single(result.Sections);
    }

    // -----------------------------------------------------------------------------------------
    // Filters: category, value filters, availability, source.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Category_filter_narrows_valid_rows_only_not_broken_rows()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "A", WidgetType, "kind-a", "1"),
                Valid("alpha", "a2", "B", WidgetType, "kind-b", "1"),
                Broken("alpha", "b1", "C", WidgetType, EntryUnresolvedReason.ValuesRejected),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Categories: ["kind-a"]));

        var section = Assert.Single(result.Sections);
        Assert.Equal(["A"], section.ValidRows.Select(row => row.Entry.Name));
        Assert.Single(section.BrokenRows);
    }

    [Fact]
    public void A_value_filter_narrows_valid_rows_by_the_profiles_own_field()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "A", WidgetType, "k", "1"),
                Valid("alpha", "a2", "B", WidgetType, "k", "2"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(
            new ContentListState(ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["2"] }));

        var section = Assert.Single(result.Sections);
        Assert.Equal(["B"], section.ValidRows.Select(row => row.Entry.Name));
    }

    [Fact]
    public void Filter_options_come_from_the_data_and_are_ordered_by_the_profiles_own_comparer()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "A", WidgetType, "k", "3"),
                Valid("alpha", "a2", "B", WidgetType, "k", "1"),
                Valid("alpha", "a3", "C", WidgetType, "k", "2"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        var levelFilter = result.ValueFilters.Single(filter => filter.Label == "Poziom");
        Assert.Equal(["1", "2", "3"], levelFilter.Options);
    }

    [Fact]
    public void A_filter_with_no_options_is_unavailable()
    {
        var registry = new ContentRegistry([], [], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        Assert.False(result.Category!.IsAvailable);
        Assert.False(result.ValueFilters.Single(filter => filter.Label == "Poziom").IsAvailable);
    }

    [Fact]
    public void Source_filter_hides_every_other_section_including_rejected_ones()
    {
        var alpha = MakePack("alpha", "Alpha", MakeEntry("a1", "A", WidgetType, new Widget("k", "1")));
        var beta = MakePack("beta", "Beta", MakeEntry("b1", "B", WidgetType, new Widget("k", "1")));
        var registry = new ContentRegistry(
            [alpha, beta],
            [Valid("alpha", "a1", "A", WidgetType, "k", "1"), Valid("beta", "b1", "B", WidgetType, "k", "1")],
            [],
            [new RejectedPack("gamma-dir", "reason")]);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Packs: ["Alpha"]));

        var section = Assert.Single(result.Sections);
        Assert.Equal("Alpha", section.Header);
    }

    [Fact]
    public void Source_options_include_rejected_packs_and_packs_with_only_broken_entries()
    {
        var alpha = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [alpha],
            [Broken("alpha", "a1", "A", WidgetType, EntryUnresolvedReason.ValuesRejected)],
            [],
            [new RejectedPack("gamma-dir", "reason")]);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        Assert.Equal(["Alpha", "gamma-dir"], result.Pack.Options);
    }

    [Fact]
    public void Clearing_filters_resets_filters_and_search_but_keeps_the_sort()
    {
        var state = new ContentListState(
            Search: "x",
            Categories: ["y"],
            Packs: ["z"],
            ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["1"] },
            Sort: "Poziom",
            SortDescending: true);

        var cleared = state.ClearFilters();

        Assert.Equal(string.Empty, cleared.Search);
        Assert.Null(cleared.Categories);
        Assert.Null(cleared.Packs);
        Assert.Null(cleared.ValueFilters);
        Assert.Equal("Poziom", cleared.Sort);
        Assert.True(cleared.SortDescending);
        Assert.False(cleared.AnyFilterNarrows);
    }

    [Fact]
    public void Only_the_search_or_a_chosen_value_narrows_never_the_sort_or_an_empty_choice()
    {
        Assert.False(new ContentListState(Sort: "Poziom").AnyFilterNarrows);
        Assert.False(new ContentListState(
            Categories: [], Packs: [], ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = [] }).AnyFilterNarrows);
        Assert.True(new ContentListState(Search: "a").AnyFilterNarrows);
        Assert.True(new ContentListState(Packs: ["Alpha"]).AnyFilterNarrows);
        Assert.True(new ContentListState(
            ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["1"] }).AnyFilterNarrows);
    }

    private static ContentRegistry ThreeTierRegistry() => new(
        [MakePack("alpha", "Alpha")],
        [
            Valid("alpha", "a1", "A", WidgetType, "kind-a", "1"),
            Valid("alpha", "a2", "B", WidgetType, "kind-b", "2"),
            Valid("alpha", "a3", "C", WidgetType, "kind-a", "3"),
        ],
        [],
        []);

    private static IReadOnlyList<string> ShownNames(ContentListResult result) =>
        result.Sections.SelectMany(section => section.ValidRows).Select(row => row.Entry.Name).ToArray();

    [Fact]
    public void Two_values_of_one_filter_show_their_union()
    {
        var result = ModelFor(WidgetTab(), ThreeTierRegistry()).Build(
            new ContentListState(ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["1", "2"] }));

        Assert.Equal(["A", "B"], ShownNames(result));
    }

    [Fact]
    public void Two_filters_show_their_intersection()
    {
        var result = ModelFor(WidgetTab(), ThreeTierRegistry()).Build(new ContentListState(
            Categories: ["kind-a"],
            ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["1", "2"] }));

        Assert.Equal(["A"], ShownNames(result));
    }

    [Fact]
    public void An_empty_choice_shows_everything()
    {
        var result = ModelFor(WidgetTab(), ThreeTierRegistry()).Build(new ContentListState(
            Categories: [],
            Packs: [],
            ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = [] }));

        Assert.Equal(["A", "B", "C"], ShownNames(result));
    }

    [Fact]
    public void Filter_options_stay_whole_while_other_filters_narrow_the_list()
    {
        var result = ModelFor(WidgetTab(), ThreeTierRegistry()).Build(new ContentListState(
            Search: "A",
            Categories: ["kind-a"],
            ValueFilters: new Dictionary<string, IReadOnlyCollection<string>> { ["Poziom"] = ["1"] }));

        Assert.Equal(["1", "2", "3"], result.ValueFilters.Single(filter => filter.Label == "Poziom").Options);
        Assert.Equal(["kind-a", "kind-b"], result.Category!.Options);
    }

    [Fact]
    public void The_category_filter_carries_the_systems_label_and_is_absent_when_no_profile_declares_one()
    {
        var withCategory = ModelFor(WidgetTab(), ThreeTierRegistry()).Build(new ContentListState());
        var withoutCategory = ModelFor(new ContentTabDefinition("Gadżety", [GadgetProfile()]), ThreeTierRegistry())
            .Build(new ContentListState());

        Assert.Equal("Rodzaj", withCategory.Category!.Label);
        Assert.Null(withoutCategory.Category);
        Assert.Equal(ContentListModel.PackFilterLabel, withCategory.Pack.Label);
    }

    // -----------------------------------------------------------------------------------------
    // Counters.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Total_count_is_independent_of_the_current_filters()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [Valid("alpha", "a1", "A", WidgetType, "k", "1"), Valid("alpha", "a2", "B", WidgetType, "k", "1")],
            [],
            []);

        var model = ModelFor(WidgetTab(), registry);
        var filtered = model.Build(new ContentListState(Search: "does-not-match"));

        Assert.Equal(2, model.TotalCount);
        Assert.Equal(2, filtered.TotalCount);
        Assert.Equal(0, filtered.ShownCount);
    }

    [Fact]
    public void Shown_count_reflects_only_currently_visible_valid_entries()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "A", WidgetType, "k", "1"),
                Valid("alpha", "a2", "B", WidgetType, "k", "1"),
                Broken("alpha", "b1", "C", WidgetType, EntryUnresolvedReason.ValuesRejected),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Search: "A"));

        Assert.Equal(1, result.ShownCount);
    }

    // -----------------------------------------------------------------------------------------
    // Sorts.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void The_default_sort_is_by_name_and_is_always_offered_first()
    {
        var registry = new ContentRegistry([], [], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        Assert.Equal("Nazwa", result.Sorts[0]);
        Assert.Equal(ContentListModel.DefaultSortLabel, result.Sorts[0]);
    }

    [Fact]
    public void Only_a_systems_sort_is_numeric_the_name_sort_is_textual()
    {
        var registry = new ContentRegistry([], [], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        Assert.Equal(["Nazwa", "Poziom"], result.Sorts);
        Assert.Equal(["Poziom"], result.NumericSorts);
    }

    [Fact]
    public void A_declared_sort_is_offered_and_applied()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Zeta-name", WidgetType, "k", "3"),
                Valid("alpha", "a2", "Alfa-name", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Sort: "Poziom"));

        Assert.Contains("Poziom", result.Sorts);
        var section = Assert.Single(result.Sections);
        Assert.Equal(["Alfa-name", "Zeta-name"], section.ValidRows.Select(row => row.Entry.Name));
    }

    // A sort whose key some entries lack ("-" here) lists those entries last in both directions,
    // among themselves by name.
    [Theory]
    [InlineData(false, new[] { "Jeden", "Trzy", "Alfa-brak", "Zeta-brak" })]
    [InlineData(true, new[] { "Trzy", "Jeden", "Alfa-brak", "Zeta-brak" })]
    public void Entries_without_the_sort_key_list_last_in_both_directions(bool descending, string[] expected)
    {
        var profile = new ContentTypeProfile<Widget>(
            WidgetType,
            sorts:
            [
                new ContentSortSpec<Widget>(
                    "Poziom",
                    (a, b) => TierOrder.Compare(a.Tier, b.Tier),
                    HasKey: widget => widget.Tier != "-"),
            ]);
        var registry = new ContentRegistry(
            [MakePack("alpha", "Alpha")],
            [
                Valid("alpha", "a1", "Zeta-brak", WidgetType, "k", "-"),
                Valid("alpha", "a2", "Trzy", WidgetType, "k", "3"),
                Valid("alpha", "a3", "Alfa-brak", WidgetType, "k", "-"),
                Valid("alpha", "a4", "Jeden", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(new ContentTabDefinition("Widgety", [profile]), registry)
            .Build(new ContentListState(Sort: "Poziom", SortDescending: descending));

        Assert.Equal(expected, Assert.Single(result.Sections).ValidRows.Select(row => row.Entry.Name));
    }

    [Fact]
    public void The_default_sort_orders_valid_rows_by_name()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Zeta", WidgetType, "k", "1"),
                Valid("alpha", "a2", "Alfa", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        var section = Assert.Single(result.Sections);
        Assert.Equal(["Alfa", "Zeta"], section.ValidRows.Select(row => row.Entry.Name));
    }

    [Fact]
    public void The_name_sort_reversed_orders_valid_rows_z_to_a()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Beta", WidgetType, "k", "1"),
                Valid("alpha", "a2", "Alfa", WidgetType, "k", "1"),
                Valid("alpha", "a3", "Zeta", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(SortDescending: true));

        var section = Assert.Single(result.Sections);
        Assert.Equal(["Zeta", "Beta", "Alfa"], section.ValidRows.Select(row => row.Entry.Name));
    }

    [Fact]
    public void A_reversed_declared_sort_reverses_the_key_but_breaks_ties_by_name_a_to_z()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Alfa", WidgetType, "k", "1"),
                Valid("alpha", "a2", "Zeta", WidgetType, "k", "3"),
                Valid("alpha", "a3", "Beta", WidgetType, "k", "3"),
                Valid("alpha", "a4", "Gamma", WidgetType, "k", "2"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Sort: "Poziom", SortDescending: true));

        var section = Assert.Single(result.Sections);
        Assert.Equal(["Beta", "Zeta", "Gamma", "Alfa"], section.ValidRows.Select(row => row.Entry.Name));
    }

    [Fact]
    public void A_broken_entry_stays_at_the_bottom_of_its_section_when_the_sort_is_reversed()
    {
        var entries = new[]
        {
            MakeEntry("a1", "Alfa", WidgetType, new Widget("k", "1")),
            MakeEntry("b1", "Beta", WidgetType, new Widget("k", "1")),
            MakeEntry("z1", "Zeta", WidgetType, new Widget("k", "1")),
        };
        var registry = new ContentRegistry(
            [MakePack("alpha", "Alpha", entries), MakePack("beta", "Beta pack")],
            [
                Broken("alpha", "z1", "Zeta", WidgetType, EntryUnresolvedReason.ValuesRejected),
                Valid("alpha", "a1", "Alfa", WidgetType, "k", "1"),
                Valid("alpha", "b1", "Beta", WidgetType, "k", "1"),
                Valid("beta", "c1", "Celsus", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(SortDescending: true));

        Assert.Equal(["Alpha", "Beta pack"], result.Sections.Select(section => section.Header));
        var alpha = result.Sections[0];
        Assert.Equal(["Beta", "Alfa"], alpha.ValidRows.Select(row => row.Entry.Name));
        Assert.Equal(["Zeta"], alpha.BrokenRows.Select(row => row.DisplayName));
    }

    /// <summary>
    /// Not ordinal: ordinal byte order puts every Polish letter after "z" ("Ż" is 0x017B, far past
    /// "z"'s 0x007A), which is not the order a Polish-reading GM expects from an alphabetised list.
    /// Confirmed against pl-PL directly, not assumed.
    /// </summary>
    [Fact]
    public void The_default_sort_uses_polish_collation_not_byte_order()
    {
        var pack = MakePack("alpha", "Alpha");
        var registry = new ContentRegistry(
            [pack],
            [
                Valid("alpha", "a1", "Żmija", WidgetType, "k", "1"),
                Valid("alpha", "a2", "Zombie", WidgetType, "k", "1"),
                Valid("alpha", "a3", "ćma", WidgetType, "k", "1"),
                Valid("alpha", "a4", "Bugbear", WidgetType, "k", "1"),
            ],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState());

        var section = Assert.Single(result.Sections);
        Assert.Equal(["Bugbear", "ćma", "Zombie", "Żmija"], section.ValidRows.Select(row => row.Entry.Name));
    }

    // -----------------------------------------------------------------------------------------
    // Selection.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Selecting_a_valid_entry_returns_its_category_name_and_tags()
    {
        var pack = MakePack("alpha", "Alpha");
        var address = new EntryAddress(ContentId.Create("alpha"), ContentId.Create("a1"));
        var registry = new ContentRegistry([pack], [Valid("alpha", "a1", "A", WidgetType, "kind", "7")], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Selected: new EntrySelectionKey(address)));

        var selection = Assert.IsType<ValidEntrySelection>(result.Selection);
        Assert.Equal("kind", selection.Category);
        Assert.Equal("A", selection.Name);
        Assert.Equal(["7"], selection.Tags);
    }

    [Fact]
    public void Selecting_a_broken_entry_returns_the_row_carrying_its_own_reason()
    {
        var pack = MakePack("alpha", "Alpha");
        var address = new EntryAddress(ContentId.Create("alpha"), ContentId.Create("a1"));
        var registry = new ContentRegistry(
            [pack], [Broken("alpha", "a1", "A", WidgetType, EntryUnresolvedReason.ValuesRejected)], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(new ContentListState(Selected: new EntrySelectionKey(address)));

        var selection = Assert.IsType<BrokenRowSelection>(result.Selection);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, selection.Row.UnresolvedEntry!.Unresolved);
    }

    [Fact]
    public void Selecting_a_rejected_pack_header_returns_it()
    {
        var registry = new ContentRegistry([], [], [], [new RejectedPack("broken-dir", "reason")]);

        var result = ModelFor(WidgetTab(), registry).Build(
            new ContentListState(Selected: new RejectedPackSelectionKey("broken-dir")));

        var selection = Assert.IsType<RejectedPackSelection>(result.Selection);
        Assert.Equal("reason", selection.Pack.Reason);
    }

    [Fact]
    public void A_selection_that_is_no_longer_visible_after_filtering_disappears()
    {
        var pack = MakePack("alpha", "Alpha");
        var address = new EntryAddress(ContentId.Create("alpha"), ContentId.Create("a1"));
        var registry = new ContentRegistry([pack], [Valid("alpha", "a1", "A", WidgetType, "k", "1")], [], []);

        var result = ModelFor(WidgetTab(), registry).Build(
            new ContentListState(Search: "does-not-match", Selected: new EntrySelectionKey(address)));

        Assert.Null(result.Selection);
    }

    [Fact]
    public void A_selection_that_is_still_visible_survives_filtering()
    {
        var pack = MakePack("alpha", "Alpha");
        var address = new EntryAddress(ContentId.Create("alpha"), ContentId.Create("a1"));
        var registry = new ContentRegistry(
            [pack],
            [Valid("alpha", "a1", "A", WidgetType, "k", "1"), Valid("alpha", "a2", "B", WidgetType, "k", "1")],
            [],
            []);

        var result = ModelFor(WidgetTab(), registry).Build(
            new ContentListState(Search: "A", Selected: new EntrySelectionKey(address)));

        Assert.NotNull(result.Selection);
    }
}
