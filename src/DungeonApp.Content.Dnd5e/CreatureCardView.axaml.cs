using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The creature statblock. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately
/// after construction, through <see cref="SetCreature"/>: the constructor stays parameterless, as the
/// XAML loader and the designer preview need it.
/// <para>
/// The portrait and the KP / PZ / Szybkość values belong to the detail header
/// (<see cref="IEntryCardHeader"/>): the header draws the title and tags for every content type
/// alike, and this card only lends it those two pieces. The portrait is DungeonDetailPictureWidth
/// wide at 3:4, and the pairs' values start on the title column's line beside it.
/// </para>
/// <para>
/// Every value on the card comes from a field <see cref="Creature"/> declares, its
/// <see cref="CombatAspect"/> included; a pair or a section whose field is empty is not shown at
/// all. Ability modifiers are D&amp;D 5e's own arithmetic (<see cref="AbilityModifier"/>), tinted
/// by their sign.
/// </para>
/// </summary>
public partial class CreatureCardView : UserControl, IEntryCardHeader
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private readonly ImageFrame _portrait;
    private readonly HeadlineValuesView _vitals = new();

    public CreatureCardView()
    {
        InitializeComponent();

        var width = ThemeResource.Get<double>("DungeonDetailPictureWidth");
        _portrait = new ImageFrame
        {
            Width = width,
            Height = width * 4 / 3,
            VerticalAlignment = VerticalAlignment.Top,
            // Sharp, like the ability tables under it: one kind of corner on the card.
            CornerRadius = default,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconSkull"),
        };

        // The pairs' values start on the title column's line: the label column and the pair's own
        // gap together span the portrait and the gap after it, a long label wrapping inside.
        Traits.LabelWidth = width + ThemeResource.Get<double>("DungeonDetailColumnGap") - ThemeResource.Get<double>("DungeonSpacingSm");
    }

    public Control HeaderVisual => _portrait;

    /// <summary>Nothing at the end of the title's line: a creature's name has the line to itself.</summary>
    public Control? HeaderTitleEnd => null;

    /// <summary>Nothing after the tags: size, type and alignment say it all.</summary>
    public Control? HeaderTagsStart => null;

    public Control HeaderBlock => _vitals;

    public void SetCreature(Creature creature, EntryPicture picture)
    {
        picture.ShowIn(_portrait);

        var combat = creature.Combat;

        _vitals.Show(
        [
            new HeadlineValue("KP", "DungeonIconShield", combat.Ac.ToString(CultureInfo.InvariantCulture), combat.AcSource),
            new HeadlineValue("PZ", "DungeonIconHeart", combat.Hp.ToString(CultureInfo.InvariantCulture), combat.HpDice),
            new HeadlineValue("Szybkość", "DungeonIconFootprints", creature.Speed),
        ]);

        PhysicalAbilities.Rows =
        [
            Ability("SIŁ", combat.Str),
            Ability("ZRĘ", combat.Dex),
            Ability("KON", combat.Con),
        ];
        MentalAbilities.Rows =
        [
            Ability("INT", combat.Int),
            Ability("MDR", combat.Wis),
            Ability("CHA", combat.Cha),
        ];

        Traits.Rows = BuildTraitRows(creature);

        Show(SpecialAbilitiesSection, creature.SpecialAbilities);
        Show(ActionsSection, creature.Actions);
        Show(SpellcastingSection, creature.Spellcasting);
        Show(BonusActionsSection, creature.BonusActions);
        Show(ReactionsSection, creature.Reactions);
        Show(LegendaryActionsSection, creature.LegendaryActions);

        Footer.IsVisible = creature.Description is not null;
        DescriptionText.Text = creature.Description;
    }

    /// <summary>
    /// The pairs in statblock order. Zmysły and Wyzwanie are required fields, so they always show;
    /// every other pair only when its field is filled. Wyzwanie is the one value the GM looks for
    /// first, so it is drawn highlighted.
    /// </summary>
    private static IReadOnlyList<TraitRow> BuildTraitRows(Creature creature)
    {
        var rows = new List<TraitRow>();

        AddIfFilled(rows, "Rzuty obronne", creature.SavingThrows);
        AddIfFilled(rows, "Umiejętności", creature.Skills);
        AddIfFilled(rows, "Podatność na obrażenia", creature.DamageVulnerabilities);
        AddIfFilled(rows, "Odporność na obrażenia", creature.DamageResistances);
        AddIfFilled(rows, "Niewrażliwość na obrażenia", creature.DamageImmunities);
        AddIfFilled(rows, "Niewrażliwość na stany", creature.ConditionImmunities);
        rows.Add(new TraitRow("Zmysły", creature.Senses));
        AddIfFilled(rows, "Języki", creature.Languages);
        rows.Add(new TraitRow(
            "Wyzwanie",
            creature.Challenge,
            creature.Xp is { } xp ? $"{xp.ToString("N0", Polish)} PD" : null,
            IsHighlighted: true));

        return rows;
    }

    private static void AddIfFilled(List<TraitRow> rows, string label, string? value)
    {
        if (value is not null)
        {
            rows.Add(new TraitRow(label, value));
        }
    }

    private static AbilityRow Ability(string label, int score)
    {
        var modifier = AbilityModifier.Compute(score);
        var tone = modifier switch
        {
            > 0 => ValueTone.Positive,
            < 0 => ValueTone.Negative,
            _ => ValueTone.Neutral,
        };

        return new AbilityRow(label, score.ToString(CultureInfo.InvariantCulture), AbilityModifier.Format(score), tone);
    }

    private static void Show(ProseSectionView view, StatblockSection? section)
    {
        view.IsVisible = section is not null;
        view.Intro = section?.Intro;
        view.Items = section is null
            ? []
            : [.. section.Entries.Select(entry => new ProseItem(entry.Name, entry.Note, entry.Text))];
    }
}
