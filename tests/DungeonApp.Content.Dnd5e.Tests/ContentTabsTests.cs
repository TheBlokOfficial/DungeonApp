using System;
using System.IO;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// The two content tab definitions this system declares as data, tested as data: what "Potwory" and
/// "Przedmioty" read off their own records, and the "Wyzwanie"/"Rzadkość" option orders.
/// </summary>
public sealed class ContentTabsTests
{
    private static readonly Dnd5eSystem Dnd5e = new(
        new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"dnd5e-content-tabs-tests-{Guid.NewGuid():N}")),
        [Path.Combine(Path.GetTempPath(), $"dnd5e-content-tabs-tests-packs-{Guid.NewGuid():N}")]);

    private static IContentTypeProfile MonsterProfile() =>
        Dnd5e.ContentTabDefinitions.Single(tab => tab.Title == "Potwory").ContentTypes.Single();

    private static IContentTypeProfile GearProfile() =>
        Dnd5e.ContentTabDefinitions.Single(tab => tab.Title == "Przedmioty").ContentTypes.Single();

    private static Entry MonsterEntry(string challenge, string size = "Mały", string type = "goblinoid", string alignment = "chaotyczne zło", string? group = null)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("monster"));
        var monster = new Monster
        {
            Size = size,
            Type = type,
            Group = group,
            Alignment = alignment,
            Ac = 10,
            Hp = 1,
            Speed = "30 stóp",
            Str = 10,
            Dex = 10,
            Con = 10,
            Int = 10,
            Wis = 10,
            Cha = 10,
            Senses = "zwykły wzrok",
            Challenge = challenge,
            Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Brak", Text = "Nic nie robi." }] },
        };
        return new Entry(ContentId.Create("m"), "Testowy potwór", reference, 1, ContentValues.From(monster));
    }

    private static Entry GearEntry(string rarity, Func<Gear, Gear>? change = null)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));
        var gear = new Gear { Rarity = rarity, Category = "Ekwipunek" };
        return new Entry(ContentId.Create("g"), "Testowy przedmiot", reference, 1, ContentValues.From(change is null ? gear : change(gear)));
    }

    // -----------------------------------------------------------------------------------------
    // Declarations: two tabs.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void The_system_declares_exactly_two_content_tabs_named_for_their_content()
    {
        Assert.Equal(["Potwory", "Przedmioty"], Dnd5e.ContentTabDefinitions.Select(tab => tab.Title));
    }

    [Fact]
    public void Each_content_tab_names_exactly_the_content_type_it_speaks_for()
    {
        var monsterReference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("monster"));
        var gearReference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));

        Assert.Equal(monsterReference, MonsterProfile().Type);
        Assert.Equal(gearReference, GearProfile().Type);
    }

    // -----------------------------------------------------------------------------------------
    // Monster profile: category, tags, badge, "Wyzwanie" filter and sort.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// A monster's category is its own optional <see cref="Monster.Group"/> field, never
    /// <see cref="Monster.Type"/> - the two are deliberately different values here so this test
    /// would fail if the profile read <c>Type</c>.
    /// </summary>
    [Fact]
    public void A_monsters_category_is_its_own_group_field()
    {
        var entry = MonsterEntry("1/4 (50 PD)", type: "humanoid (goblinoid)", group: "Goblinoidy");

        Assert.Equal("Goblinoidy", MonsterProfile().Category(entry));
    }

    /// <summary>
    /// Without <c>group</c> an entry has no category - never a silent fallback to
    /// <see cref="Monster.Type"/>.
    /// </summary>
    [Fact]
    public void The_monster_category_filter_is_named_grupa_and_gear_has_none()
    {
        Assert.Equal("Grupa", MonsterProfile().CategoryLabel);
        Assert.Null(GearProfile().CategoryLabel);
    }

    [Fact]
    public void A_monsters_value_filters_are_type_then_challenge_and_type_orders_alphabetically_in_polish()
    {
        Assert.Equal(["Typ", "Wyzwanie"], MonsterProfile().ValueFilters.Select(filter => filter.Label));

        var typeFilter = MonsterProfile().ValueFilters[0];
        var entry = MonsterEntry("1/4 (50 PD)", type: "humanoid (goblinoid)");
        var sorted = new[] { "żywiołak", "smok", "bestia", "ślimak" }.OrderBy(value => value, typeFilter.OptionOrder);

        Assert.Equal("humanoid (goblinoid)", typeFilter.Value(entry));
        Assert.Equal(["bestia", "smok", "ślimak", "żywiołak"], sorted);
    }

    [Fact]
    public void A_monster_with_no_group_has_no_category()
    {
        var entry = MonsterEntry("1/4 (50 PD)", type: "humanoid (goblinoid)", group: null);

        Assert.Null(MonsterProfile().Category(entry));
    }

    [Fact]
    public void A_monsters_tags_are_size_type_and_alignment_in_that_order()
    {
        var entry = MonsterEntry("1/4 (50 PD)", size: "Mały", type: "humanoid (goblinoid)", alignment: "neutralny zły");

        Assert.Equal(["Mały", "humanoid (goblinoid)", "neutralny zły"], MonsterProfile().Tags(entry));
    }

    [Fact]
    public void A_monsters_badge_is_its_challenge_with_no_color_key()
    {
        var entry = MonsterEntry("1/2 (100 PD)");

        var badge = MonsterProfile().Badge(entry);

        Assert.Equal("1/2 (100 PD)", badge.Text);
        Assert.Null(badge.ColorKey);
    }

    [Fact]
    public void The_challenge_filter_and_sort_order_fractions_numerically()
    {
        var order = Assert.Single(MonsterProfile().ValueFilters, filter => filter.Label == "Wyzwanie").OptionOrder;

        var values = new[] { "1/2 (100 PD)", "1/8 (25 PD)", "1/4 (50 PD)", "5 (1 800 PD)" };
        var sorted = values.OrderBy(value => value, order).ToArray();

        Assert.Equal(["1/8 (25 PD)", "1/4 (50 PD)", "1/2 (100 PD)", "5 (1 800 PD)"], sorted);
    }

    [Fact]
    public void An_unparsable_challenge_value_sorts_after_every_numeric_one_alphabetically()
    {
        var order = Assert.Single(MonsterProfile().ValueFilters, filter => filter.Label == "Wyzwanie").OptionOrder;

        var values = new[] { "zmienne", "1 (200 PD)", "nieznane" };
        var sorted = values.OrderBy(value => value, order).ToArray();

        Assert.Equal(["1 (200 PD)", "nieznane", "zmienne"], sorted);
    }

    [Fact]
    public void The_challenge_sort_orders_monster_entries_the_same_way_as_the_filter()
    {
        var sort = Assert.Single(MonsterProfile().Sorts, sort => sort.Label == "Wyzwanie");

        var high = MonsterEntry("1/2 (100 PD)");
        var low = MonsterEntry("1/8 (25 PD)");

        Assert.True(sort.Compare(low, high) < 0);
        Assert.True(sort.Compare(high, low) > 0);
    }

    // -----------------------------------------------------------------------------------------
    // Gear profile: no category, tags, badge with a color key per tier, "Rzadkość" filter.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Gear_has_no_category()
    {
        var entry = GearEntry("Pospolity");

        Assert.Null(GearProfile().Category(entry));
    }

    [Fact]
    public void An_items_tags_are_its_category_then_its_subtype_when_it_has_one()
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));
        var sword = new Entry(ContentId.Create("s"), "Miecz", reference, 1, ContentValues.From(
            new Gear { Rarity = "Zwykły", Category = "Broń", Subtype = "miecz długi" }));

        Assert.Equal(["Broń", "miecz długi"], GearProfile().Tags(sword));
        Assert.Equal(["Ekwipunek"], GearProfile().Tags(GearEntry("Rzadki")));
    }

    [Fact]
    public void A_recognised_rarity_tier_carries_a_color_key_on_its_badge()
    {
        var entry = GearEntry("Niezwykły");

        var badge = GearProfile().Badge(entry);

        Assert.Equal("Niezwykły", badge.Text);
        Assert.NotNull(badge.ColorKey);
    }

    [Fact]
    public void An_unrecognised_rarity_carries_no_color_key()
    {
        var entry = GearEntry("Coś nowego");

        var badge = GearProfile().Badge(entry);

        Assert.Equal("Coś nowego", badge.Text);
        Assert.Null(badge.ColorKey);
    }

    [Fact]
    public void The_rarity_filter_orders_tiers_from_mundane_to_artifact()
    {
        var order = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Rzadkość").OptionOrder;

        var shuffled = new[] { "Legendarny", "Pospolity", "Artefakt", "Zwykły", "Bardzo rzadki", "Rzadki", "Niezwykły" };
        var sorted = shuffled.OrderBy(value => value, order).ToArray();

        Assert.Equal(["Zwykły", "Pospolity", "Niezwykły", "Rzadki", "Bardzo rzadki", "Legendarny", "Artefakt"], sorted);
    }

    [Fact]
    public void A_mundane_item_carries_its_own_color_key_apart_from_common()
    {
        var mundane = GearProfile().Badge(GearEntry("Zwykły")).ColorKey;
        var common = GearProfile().Badge(GearEntry("Pospolity")).ColorKey;

        Assert.NotNull(mundane);
        Assert.NotEqual(common, mundane);
        Assert.NotNull(Dnd5e.ResolveBadgeBrush(mundane!));
    }

    [Fact]
    public void An_unknown_rarity_sorts_after_every_known_tier_alphabetically()
    {
        var order = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Rzadkość").OptionOrder;

        var values = new[] { "Zjawiskowy", "Pospolity", "Abstrakcyjny" };
        var sorted = values.OrderBy(value => value, order).ToArray();

        Assert.Equal(["Pospolity", "Abstrakcyjny", "Zjawiskowy"], sorted);
    }

    [Fact]
    public void The_rarity_sort_orders_gear_entries_the_same_way_as_the_filter()
    {
        var sort = Assert.Single(GearProfile().Sorts, sort => sort.Label == "Rzadkość");

        Assert.False(sort.IsTextual);
        Assert.True(sort.Compare(GearEntry("Pospolity"), GearEntry("Rzadki")) < 0);
        Assert.True(sort.Compare(GearEntry("Rzadki"), GearEntry("Niezwykły")) > 0);
    }

    [Fact]
    public void Items_offer_exactly_the_rarity_and_attunement_filters_and_the_rarity_and_worth_sorts()
    {
        Assert.Equal(["Rzadkość", "Dostrojenie"], GearProfile().ValueFilters.Select(filter => filter.Label));
        Assert.Equal(["Rzadkość", "Wartość"], GearProfile().Sorts.Select(sort => sort.Label));
    }

    [Fact]
    public void The_attunement_filter_reads_needs_or_needs_not_and_lists_needs_first()
    {
        var filter = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Dostrojenie");

        Assert.Equal("wymaga", filter.Value(GearEntry("Rzadki", gear => gear with { Attunement = true })));
        Assert.Equal("nie wymaga", filter.Value(GearEntry("Rzadki")));
        Assert.Equal(["wymaga", "nie wymaga"], new[] { "nie wymaga", "wymaga" }.OrderBy(value => value, filter.OptionOrder));
    }

    [Fact]
    public void The_worth_sort_orders_by_number_and_knows_an_item_without_worth_has_no_key()
    {
        var sort = Assert.Single(GearProfile().Sorts, sort => sort.Label == "Wartość");
        var cheap = GearEntry("Zwykły", gear => gear with { Value = 0.5m });
        var dear = GearEntry("Zwykły", gear => gear with { Value = 15m });
        var priceless = GearEntry("Zwykły");

        Assert.False(sort.IsTextual);
        Assert.True(sort.Compare(cheap, dear) < 0);
        Assert.True(sort.HasKey!(cheap));
        Assert.False(sort.HasKey!(priceless));
    }
}
