using System;
using System.IO;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// The two content tab definitions this system declares as data, tested as data: what "Stworzenia" and
/// "Przedmioty" read off their own records, and the "Wyzwanie"/"Rzadkość" option orders.
/// </summary>
public sealed class ContentTabsTests
{
    private static readonly Dnd5eSystem Dnd5e = new(
        new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"dnd5e-content-tabs-tests-{Guid.NewGuid():N}")),
        [Path.Combine(Path.GetTempPath(), $"dnd5e-content-tabs-tests-packs-{Guid.NewGuid():N}")]);

    private static IContentTypeProfile CreatureProfile() =>
        Dnd5e.ContentTabDefinitions.Single(tab => tab.Title == "Stworzenia").ContentTypes.Single();

    private static IContentTypeProfile GearProfile() =>
        Dnd5e.ContentTabDefinitions.Single(tab => tab.Title == "Przedmioty").ContentTypes.Single();

    private static Entry CreatureEntry(string challenge, string size = "Mały", string type = "goblinoid", string alignment = "chaotyczne zło", string? group = null)
    {
        var creature = new Creature
        {
            Size = size,
            Type = type,
            Group = group,
            Alignment = alignment,
            Combat = new CombatAspect { Ac = 10, Hp = 1, Str = 10, Dex = 10, Con = 10, Int = 10, Wis = 10, Cha = 10 },
            Speed = "30 stóp",
            Senses = "zwykły wzrok",
            Challenge = challenge,
            Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Brak", Text = "Nic nie robi." }] },
        };
        return CreatureEntry(creature);
    }

    private static Entry CreatureEntry(Creature creature)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("creature"));
        return new Entry(ContentId.Create("c"), "Testowe stworzenie", reference, 1, ContentValues.From(creature));
    }

    /// <summary>An innkeeper: no combat aspect, so no challenge either.</summary>
    private static Entry InnkeeperEntry() => CreatureEntry(new Creature
    {
        Size = "Średni",
        Type = "humanoid (człowiek)",
        Alignment = "praworządny neutralny",
        Speed = "9 m",
        Senses = "bierna Percepcja 12",
    });

    private static Entry GearEntry(string rarity, Func<Gear, Gear>? change = null)
    {
        var reference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));
        var gear = new Gear { Rarity = rarity, Category = "Ekwipunek", Item = new ItemAspect { Weight = 1, Value = 1 } };
        return new Entry(ContentId.Create("g"), "Testowy przedmiot", reference, 1, ContentValues.From(change is null ? gear : change(gear)));
    }

    // -----------------------------------------------------------------------------------------
    // Declarations: two tabs.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void The_system_declares_exactly_two_content_tabs_named_for_their_content()
    {
        Assert.Equal(["Stworzenia", "Przedmioty"], Dnd5e.ContentTabDefinitions.Select(tab => tab.Title));
    }

    [Fact]
    public void Each_content_tab_names_exactly_the_content_type_it_speaks_for()
    {
        var creatureReference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("creature"));
        var gearReference = new ContentTypeReference(Dnd5e.ContentSetId, ContentId.Create("gear"));

        Assert.Equal(creatureReference, CreatureProfile().Type);
        Assert.Equal(gearReference, GearProfile().Type);
    }

    // -----------------------------------------------------------------------------------------
    // Creature profile: category, tags, badge, "Wyzwanie" filter and sort.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// A creature's category is its own optional <see cref="Creature.Group"/> field, never
    /// <see cref="Creature.Type"/> - the two are deliberately different values here so this test
    /// would fail if the profile read <c>Type</c>.
    /// </summary>
    [Fact]
    public void A_creatures_category_is_its_own_group_field()
    {
        var entry = CreatureEntry("1/4 (50 PD)", type: "humanoid (goblinoid)", group: "Goblinoidy");

        Assert.Equal("Goblinoidy", CreatureProfile().Category(entry));
    }

    /// <summary>
    /// Without <c>group</c> an entry has no category - never a silent fallback to
    /// <see cref="Creature.Type"/>.
    /// </summary>
    [Fact]
    public void The_creature_category_filter_is_named_grupa_and_the_items_kategoria()
    {
        Assert.Equal("Grupa", CreatureProfile().CategoryLabel);
        Assert.Equal("Kategoria", GearProfile().CategoryLabel);
    }

    [Fact]
    public void A_creatures_value_filters_are_type_then_challenge_and_type_orders_alphabetically_in_polish()
    {
        Assert.Equal(["Typ", "Wyzwanie"], CreatureProfile().ValueFilters.Select(filter => filter.Label));

        var typeFilter = CreatureProfile().ValueFilters[0];
        var entry = CreatureEntry("1/4 (50 PD)", type: "humanoid (goblinoid)");
        var sorted = new[] { "żywiołak", "smok", "bestia", "ślimak" }.OrderBy(value => value, typeFilter.OptionOrder);

        Assert.Equal("humanoid (goblinoid)", typeFilter.Value(entry));
        Assert.Equal(["bestia", "smok", "ślimak", "żywiołak"], sorted);
    }

    [Fact]
    public void A_creature_with_no_group_has_no_category()
    {
        var entry = CreatureEntry("1/4 (50 PD)", type: "humanoid (goblinoid)", group: null);

        Assert.Null(CreatureProfile().Category(entry));
    }

    [Fact]
    public void A_creatures_tags_are_size_type_and_alignment_in_that_order()
    {
        var entry = CreatureEntry("1/4 (50 PD)", size: "Mały", type: "humanoid (goblinoid)", alignment: "neutralny zły");

        Assert.Equal(["Mały", "humanoid (goblinoid)", "neutralny zły"], CreatureProfile().Tags(entry));
    }

    [Fact]
    public void A_creatures_badge_is_its_challenge_with_no_color_key()
    {
        var entry = CreatureEntry("1/2 (100 PD)");

        var badge = CreatureProfile().Badge(entry);

        Assert.Equal("1/2 (100 PD)", badge.Text);
        Assert.Null(badge.ColorKey);
    }

    [Fact]
    public void The_challenge_filter_and_sort_order_fractions_numerically()
    {
        var order = Assert.Single(CreatureProfile().ValueFilters, filter => filter.Label == "Wyzwanie").OptionOrder;

        var values = new[] { "1/2 (100 PD)", "1/8 (25 PD)", "1/4 (50 PD)", "5 (1 800 PD)" };
        var sorted = values.OrderBy(value => value, order).ToArray();

        Assert.Equal(["1/8 (25 PD)", "1/4 (50 PD)", "1/2 (100 PD)", "5 (1 800 PD)"], sorted);
    }

    [Fact]
    public void An_unparsable_challenge_value_sorts_after_every_numeric_one_alphabetically()
    {
        var order = Assert.Single(CreatureProfile().ValueFilters, filter => filter.Label == "Wyzwanie").OptionOrder;

        var values = new[] { "zmienne", "1 (200 PD)", "nieznane" };
        var sorted = values.OrderBy(value => value, order).ToArray();

        Assert.Equal(["1 (200 PD)", "nieznane", "zmienne"], sorted);
    }

    [Fact]
    public void The_challenge_sort_orders_creature_entries_the_same_way_as_the_filter()
    {
        var sort = Assert.Single(CreatureProfile().Sorts, sort => sort.Label == "Wyzwanie");

        var high = CreatureEntry("1/2 (100 PD)");
        var low = CreatureEntry("1/8 (25 PD)");

        Assert.True(sort.Compare(low, high) < 0);
        Assert.True(sort.Compare(high, low) > 0);
    }

    /// <summary>
    /// A creature without a statblock has no challenge: no badge, no "Wyzwanie" option, and the sort
    /// knows it has no key, so it lists after every creature that has one.
    /// </summary>
    [Fact]
    public void A_creature_without_a_challenge_has_no_badge_no_challenge_option_and_no_challenge_sort_key()
    {
        var innkeeper = InnkeeperEntry();
        var sort = Assert.Single(CreatureProfile().Sorts, sort => sort.Label == "Wyzwanie");
        var filter = Assert.Single(CreatureProfile().ValueFilters, filter => filter.Label == "Wyzwanie");

        Assert.Null(CreatureProfile().Badge(innkeeper).Text);
        Assert.Null(filter.Value(innkeeper));
        Assert.False(sort.HasKey!(innkeeper));
        Assert.True(sort.HasKey(CreatureEntry("1/8 (25 PD)")));
    }

    // -----------------------------------------------------------------------------------------
    // Gear profile: category, tags, badge with a color key per tier, filters and sorts.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void An_items_category_is_the_tabs_category()
    {
        Assert.Equal("Kategoria", GearProfile().CategoryLabel);
        Assert.Equal("Ekwipunek", GearProfile().Category(GearEntry("Pospolity")));
    }

    [Fact]
    public void An_items_tags_are_magical_then_its_subtype_each_only_when_it_has_one()
    {
        var sword = GearEntry("Pospolity", gear => gear with { Category = "Broń", Subtype = "żołnierska, do walki wręcz" });
        var staff = GearEntry("Rzadki", gear => gear with { Magical = true, Subtype = "kostur" });

        Assert.Equal(["żołnierska, do walki wręcz"], GearProfile().Tags(sword));
        Assert.Equal(["magiczny", "kostur"], GearProfile().Tags(staff));
        Assert.Equal(["magiczny"], GearProfile().Tags(GearEntry("Rzadki", gear => gear with { Magical = true })));
        Assert.Empty(GearProfile().Tags(GearEntry("Rzadki")));
    }

    [Theory]
    [InlineData("Pospolity")]
    [InlineData("Niepospolity")]
    [InlineData("Rzadki")]
    [InlineData("Epicki")]
    [InlineData("Legendarny")]
    public void Every_rarity_tier_carries_its_own_color_key_with_a_brush(string tier)
    {
        var badge = GearProfile().Badge(GearEntry(tier));

        Assert.Equal(tier, badge.Text);
        Assert.NotNull(badge.ColorKey);
        Assert.NotNull(Dnd5e.ResolveBadgeBrush(badge.ColorKey!));
    }

    [Fact]
    public void No_two_rarity_tiers_share_a_color_key()
    {
        string[] tiers = ["Pospolity", "Niepospolity", "Rzadki", "Epicki", "Legendarny"];

        var keys = tiers.Select(tier => GearProfile().Badge(GearEntry(tier)).ColorKey).Distinct();

        Assert.Equal(tiers.Length, keys.Count());
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
    public void The_rarity_filter_orders_tiers_from_common_to_legendary()
    {
        var order = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Rzadkość").OptionOrder;

        var shuffled = new[] { "Legendarny", "Pospolity", "Epicki", "Rzadki", "Niepospolity" };
        var sorted = shuffled.OrderBy(value => value, order).ToArray();

        Assert.Equal(["Pospolity", "Niepospolity", "Rzadki", "Epicki", "Legendarny"], sorted);
    }

    /// <summary>The rulebook's own tier names are not on this scale: no color, after every tier.</summary>
    [Fact]
    public void The_rulebooks_tier_names_are_not_tiers_of_this_scale()
    {
        var order = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Rzadkość").OptionOrder;

        Assert.Null(GearProfile().Badge(GearEntry("Bardzo rzadki")).ColorKey);
        Assert.True(order.Compare("Legendarny", "Artefakt") < 0);
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
        Assert.True(sort.Compare(GearEntry("Rzadki"), GearEntry("Niepospolity")) > 0);
    }

    [Fact]
    public void Items_offer_exactly_the_rarity_magic_and_attunement_filters_and_the_rarity_and_worth_sorts()
    {
        Assert.Equal(["Rzadkość", "Magia", "Dostrojenie"], GearProfile().ValueFilters.Select(filter => filter.Label));
        Assert.Equal(["Rzadkość", "Wartość"], GearProfile().Sorts.Select(sort => sort.Label));
    }

    [Fact]
    public void The_magic_filter_reads_magical_or_not_and_lists_magical_first()
    {
        var filter = Assert.Single(GearProfile().ValueFilters, filter => filter.Label == "Magia");

        Assert.Equal("magiczny", filter.Value(GearEntry("Rzadki", gear => gear with { Magical = true })));
        Assert.Equal("niemagiczny", filter.Value(GearEntry("Rzadki")));
        Assert.Equal(["magiczny", "niemagiczny"], new[] { "niemagiczny", "magiczny" }.OrderBy(value => value, filter.OptionOrder));
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
    public void The_worth_sort_orders_by_number_with_zero_first()
    {
        var sort = Assert.Single(GearProfile().Sorts, sort => sort.Label == "Wartość");
        var worthless = GearEntry("Pospolity", gear => gear with { Item = gear.Item with { Value = 0m } });
        var cheap = GearEntry("Pospolity", gear => gear with { Item = gear.Item with { Value = 0.5m } });
        var dear = GearEntry("Pospolity", gear => gear with { Item = gear.Item with { Value = 15m } });

        Assert.False(sort.IsTextual);
        Assert.True(sort.Compare(worthless, cheap) < 0);
        Assert.True(sort.Compare(cheap, dear) < 0);
    }
}
