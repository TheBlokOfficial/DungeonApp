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
        Actions = "Ugryzienie.",
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
        SpecialAbilities = "Taktyka stada.",
        Spellcasting = "Bez ograniczeń: światło.",
        BonusActions = "Odskok.",
        Reactions = "Parowanie.",
        LegendaryActions = "Kopyta.",
        Description = "Poluje w watahach.",
    };

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
        [.. card.GetVisualDescendants().OfType<Expander>().Where(section => section.IsVisible).Select(section => (string)section.Header!)];

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

        var portrait = card.FindControl<ImageFrame>("Portrait")!;
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
            ["Cechy szczególne", "Akcje", "Rzucanie czarów", "Akcje dodatkowe", "Reakcje", "Akcje legendarne", "Opis"],
            VisibleSections(card));

        var challenge = card.FindControl<TraitListView>("Traits")!.Rows.Last();
        Assert.Equal("1 800 PD", challenge.Secondary!.Replace(' ', ' ').Replace(' ', ' '));

        Assert.All(card.GetVisualDescendants().OfType<Expander>(), section => Assert.True(section.IsExpanded));

        window.Close();
    }

    [AvaloniaFact]
    public void A_picture_missing_from_the_pack_puts_the_portrait_in_its_error_state()
    {
        var card = MonsterCard(Required, new EntryPicture(null, "obrazy/wilk.png"));
        var window = Show(card);

        var portrait = card.FindControl<ImageFrame>("Portrait")!;
        Assert.Contains(":error", portrait.Classes);
        Assert.Equal(EntryPicture.MissingMessage, portrait.Message);
        Assert.Equal("obrazy/wilk.png", portrait.Detail);

        window.Close();
    }

    [AvaloniaFact]
    public void A_gear_card_builds_with_its_picture_frame()
    {
        var card = new GearCardView();
        card.SetGear(new Gear { Rarity = "Pospolity", Weight = 1, Description = "Leczy 2k4+2." }, EntryPicture.None);
        var window = Show(card);

        Assert.Single(card.GetVisualDescendants().OfType<ImageFrame>());

        window.Close();
    }
}
