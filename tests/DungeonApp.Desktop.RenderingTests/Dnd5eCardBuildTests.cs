using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The D&amp;D 5e cards built in a window: a mistyped resource or a broken template in a card
/// compiles and only fails when the card is created. Builds the monster card with only its required
/// fields and with every field, in each state of its portrait, and the gear card - no assertions
/// about looks, only about which pairs and sections a card shows.
/// </summary>
public sealed class Dnd5eCardBuildTests
{
    private static readonly Monster Required = new()
    {
        Size = "Średni",
        Type = "bestia",
        Alignment = "bez charakteru",
        Ac = 13,
        Hp = 11,
        Speed = "12 m",
        Str = 12,
        Dex = 15,
        Con = 12,
        Int = 3,
        Wis = 12,
        Cha = 6,
        Senses = "bierna Percepcja 13",
        Challenge = "1/4",
        Actions = Section("Ugryzienie"),
    };

    private static readonly Monster Full = Required with
    {
        AcSource = "pancerz naturalny",
        HpDice = "2k8+2",
        SavingThrows = "Kon +4",
        Skills = "Percepcja +3",
        DamageVulnerabilities = "obuchowe",
        DamageResistances = "od zimna",
        DamageImmunities = "od trucizny",
        ConditionImmunities = "zatrucie",
        Languages = "wspólny",
        Xp = 1800,
        SpecialAbilities = Section("Taktyka stada"),
        Spellcasting = new StatblockSection { Intro = "Rzuca czary bez komponentów.", Entries = [new StatblockEntry { Name = "Bez ograniczeń", Text = "światło" }] },
        BonusActions = Section("Odskok"),
        Reactions = Section("Parowanie"),
        LegendaryActions = Section("Kopyta"),
        Description = "Poluje w watahach.",
    };

    private static StatblockSection Section(string name) =>
        new() { Entries = [new StatblockEntry { Name = name, Note = "3 na dzień", Text = $"{name}: **+4 do trafienia**." }] };

    private static Window Show(Control content)
    {
        var window = new Window { Width = 1000, Height = 1400, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static MonsterCardView MonsterCard(Monster monster, EntryPicture picture)
    {
        var card = new MonsterCardView();
        card.SetMonster(monster, picture);
        return card;
    }

    private static string[] VisibleSections(Control card) =>
        [.. card.GetVisualDescendants().OfType<ProseSectionView>().Where(section => section.IsVisible).Select(section => section.Title!)];

    private static string[] TraitLabels(Control card) =>
        [.. card.FindControl<TraitListView>("Traits")!.Rows.Select(row => row.Label)];

    [AvaloniaFact]
    public void A_monster_with_only_required_fields_shows_only_the_required_pairs_and_sections()
    {
        var card = MonsterCard(Required, EntryPicture.None);
        var window = Show(card);

        Assert.Equal(["Zmysły", "Wyzwanie"], TraitLabels(card));
        Assert.Null(card.FindControl<TraitListView>("Traits")!.Rows.Last().Secondary);
        Assert.Equal(["Akcje"], VisibleSections(card));
        Assert.False(card.FindControl<Control>("Footer")!.IsVisible);

        var portrait = Assert.IsType<ImageFrame>(((IEntryCardHeader)card).HeaderVisual);
        Assert.DoesNotContain(":picture", portrait.Classes);
        Assert.DoesNotContain(":error", portrait.Classes);

        window.Close();
    }

    [AvaloniaFact]
    public void A_monster_with_every_field_shows_every_pair_and_section_in_statblock_order()
    {
        var card = MonsterCard(Full, EntryPicture.None);
        var window = Show(card);

        Assert.Equal(
            [
                "Rzuty obronne", "Umiejętności", "Podatność na obrażenia", "Odporność na obrażenia",
                "Niewrażliwość na obrażenia", "Niewrażliwość na stany", "Zmysły", "Języki", "Wyzwanie",
            ],
            TraitLabels(card));
        Assert.Equal(
            ["Cechy szczególne", "Akcje", "Rzucanie czarów", "Akcje dodatkowe", "Reakcje", "Akcje legendarne"],
            VisibleSections(card));

        var challenge = card.FindControl<TraitListView>("Traits")!.Rows.Last();
        Assert.Equal("1 800 PD", challenge.Secondary!.Replace(' ', ' ').Replace(' ', ' '));

        Assert.True(card.FindControl<Control>("Footer")!.IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void A_picture_missing_from_the_pack_puts_the_portrait_in_its_error_state()
    {
        var card = MonsterCard(Required, new EntryPicture(null, "obrazy/wilk.png"));
        var window = Show(card);

        var portrait = Assert.IsType<ImageFrame>(((IEntryCardHeader)card).HeaderVisual);
        Assert.Contains(":error", portrait.Classes);
        Assert.Equal(EntryPicture.MissingMessage, portrait.Message);
        Assert.Equal("obrazy/wilk.png", portrait.Detail);

        window.Close();
    }

    // The pieces a card lends the detail header are controls of their own, outside the card's tree,
    // so building the card does not build them: they are placed in a window here.
    [AvaloniaFact]
    public void The_pieces_a_monster_card_lends_the_header_build_with_its_headline_values()
    {
        var card = MonsterCard(Full, EntryPicture.None);
        var header = (IEntryCardHeader)card;
        var window = Show(new StackPanel { Children = { header.HeaderVisual!, header.HeaderBlock! } });

        var tiles = header.HeaderBlock!.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(["KP", "PZ", "Szybkość"], tiles.Select(tile => tile.Label));
        Assert.Equal(["13", "11", "12 m"], tiles.Select(tile => tile.Value));
        Assert.Equal(["pancerz naturalny", "2k8+2", null], tiles.Select(tile => tile.Note));
        Assert.All(tiles, tile => Assert.NotEmpty(tile.GetVisualChildren()));

        // The note's line is kept without a note, so Szybkość is as tall as KP and PZ.
        Assert.Single(tiles.Select(tile => tile.DesiredSize.Height).Distinct());

        window.Close();
    }

    private static readonly Gear MinimalGear = new() { Rarity = "Pospolity", Category = "Ekwipunek" };

    private static readonly Gear FullGear = MinimalGear with
    {
        Rarity = "Rzadki",
        Magical = true,
        Subtype = "kostur",
        Attunement = true,
        AttunementBy = "przez czarodzieja",
        Weight = 1.5m,
        Value = 1500,
        Damage = "1k6",
        DamageType = "obuchowe",
        Properties = "uniwersalna (1k8)",
        ArmorClass = "11",
        ArmorClassNote = "+ mod. Zr",
        StrengthRequirement = 13,
        StealthDisadvantage = true,
        Charges = 7,
        Recharge = "odzyskuje 1k6+1 ładunków o świcie",
        Description = "Gładki kij.",
    };

    private static GearCardView GearCard(Gear gear)
    {
        var card = new GearCardView();
        card.SetGear(gear, EntryPicture.None);
        return card;
    }

    private static string[] GearTraitLabels(Control card) =>
        card.FindControl<Control>("TraitsBlock")!.IsVisible
            ? [.. card.FindControl<TraitListView>("Traits")!.Rows.Select(row => row.Label)]
            : [];

    private static string[] TitleEndTexts(IEntryCardHeader header) =>
    [
        .. header.HeaderTitleEnd!.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Where(text => text.IsEffectivelyVisible)
            .Select(text => Plain(text.Text!)),
    ];

    // Digit groups and units are parted by non-breaking spaces; the expectations use plain ones.
    private static string Plain(string text) => text.Replace((char)0x00A0, ' ').Replace((char)0x202F, ' ');

    // The header's pieces are placed in the window next to the card: a control has one parent, and
    // they are not in the card's own tree.
    private static Window ShowGear(GearCardView card)
    {
        var header = (IEntryCardHeader)card;
        var panel = new StackPanel();
        foreach (var piece in new[] { header.HeaderVisual, header.HeaderTitleEnd, header.HeaderTagsStart, header.HeaderBlock })
        {
            if (piece is not null)
            {
                panel.Children.Add(piece);
            }
        }

        panel.Children.Add(card);
        return Show(panel);
    }

    [AvaloniaFact]
    public void An_item_with_every_field_shows_its_weight_and_worth_its_rarity_its_headline_values_every_pair_and_its_description()
    {
        var card = GearCard(FullGear);
        var header = (IEntryCardHeader)card;
        var window = ShowGear(card);

        Assert.IsType<ImageFrame>(header.HeaderVisual);
        Assert.Equal(["1,5 kg", "wartość 1 500"], TitleEndTexts(header));

        var rarity = Assert.IsType<WordTag>(header.HeaderTagsStart);
        Assert.Equal("Rzadki", rarity.Content);
        Assert.Contains("custom", rarity.Classes);

        var tiles = header.HeaderBlock!.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(["KP", "Obrażenia", "Ładunki"], tiles.Select(tile => tile.Label));
        Assert.Equal(["11", "1k6", "7"], tiles.Select(tile => tile.Value));
        Assert.Equal(["+ mod. Zr", "obuchowe", null], tiles.Select(tile => tile.Note));
        Assert.All(tiles, tile => Assert.NotNull(tile.Icon));

        Assert.Equal(["Dostrojenie", "Właściwości", "Siła", "Skradanie się", "Odnawianie"], GearTraitLabels(card));
        Assert.Equal("wymagane przez czarodzieja", card.FindControl<TraitListView>("Traits")!.Rows.First().Value);

        Assert.True(card.FindControl<Control>("DescriptionBlock")!.IsVisible);
        var description = card.FindControl<ProseSectionView>("DescriptionSection")!;
        Assert.Equal(("Opis", "Gładki kij."), (description.Title, description.Intro));

        window.Close();
    }

    [AvaloniaFact]
    public void An_item_with_only_required_fields_lends_no_title_end_and_no_block_and_shows_no_pairs_or_description()
    {
        var card = GearCard(MinimalGear);
        var header = (IEntryCardHeader)card;
        var window = ShowGear(card);

        Assert.Null(header.HeaderTitleEnd);
        Assert.Null(header.HeaderBlock);
        Assert.Equal("Pospolity", Assert.IsType<WordTag>(header.HeaderTagsStart).Content);
        Assert.Empty(GearTraitLabels(card));
        Assert.False(card.FindControl<Control>("DescriptionBlock")!.IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void An_item_with_only_headline_values_shows_the_filled_ones_from_the_left_and_no_pairs()
    {
        var card = GearCard(MinimalGear with { Damage = "1k8", Charges = 3 });
        var header = (IEntryCardHeader)card;
        var window = ShowGear(card);

        var tiles = header.HeaderBlock!.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(["Obrażenia", "Ładunki"], tiles.Select(tile => tile.Label));
        Assert.Equal([0, 1], tiles.Select(Grid.GetColumn));
        Assert.Equal([null, null], tiles.Select(tile => tile.Note));
        Assert.Empty(GearTraitLabels(card));

        window.Close();
    }

    [AvaloniaFact]
    public void An_item_without_headline_values_lends_no_block_but_shows_its_pairs()
    {
        var card = GearCard(MinimalGear with { Attunement = true, Properties = "lekka" });
        var header = (IEntryCardHeader)card;
        var window = ShowGear(card);

        Assert.Null(header.HeaderBlock);
        Assert.Equal(["Dostrojenie", "Właściwości"], GearTraitLabels(card));
        Assert.Equal("wymagane", card.FindControl<TraitListView>("Traits")!.Rows.First().Value);

        window.Close();
    }

    [AvaloniaFact]
    public void Without_a_weight_the_worth_alone_ends_the_title()
    {
        var card = GearCard(MinimalGear with { Value = 75 });
        var window = ShowGear(card);

        Assert.Equal(["wartość 75"], TitleEndTexts(card));

        window.Close();
    }

    // The worth is a bare number, no unit and no rounding: Polish grouping, decimal comma, no
    // trailing zeros.
    [AvaloniaTheory]
    [InlineData("15", "wartość 15")]
    [InlineData("15.00", "wartość 15")]
    [InlineData("0.5", "wartość 0,5")]
    [InlineData("1500", "wartość 1 500")]
    [InlineData("0.0125", "wartość 0,0125")]
    public void The_worth_is_written_as_a_bare_polish_number(string value, string expected)
    {
        var card = GearCard(MinimalGear with { Weight = 1, Value = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture) });
        var window = ShowGear(card);

        Assert.Equal(expected, TitleEndTexts(card)[1]);

        window.Close();
    }

    [AvaloniaFact]
    public void An_unknown_rarity_is_a_plain_word_tag()
    {
        var card = GearCard(MinimalGear with { Rarity = "Coś nowego" });

        Assert.DoesNotContain("custom", Assert.IsType<WordTag>(((IEntryCardHeader)card).HeaderTagsStart).Classes);
    }
}
