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
/// The monster statblock. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately
/// after construction, through <see cref="SetMonster"/>: the constructor stays parameterless, as the
/// XAML loader and the designer preview need it.
/// <para>
/// The portrait and the KP / PZ / Szybkość tiles belong to the detail header
/// (<see cref="IEntryCardHeader"/>): the header draws the title and tags for every content type
/// alike, and this card only lends it those two pieces. The portrait is three ability cells wide
/// (DungeonAbilityCellSize) and four tall, so the ability table under it is exactly as wide and the
/// second table starts on the title column's line - the grid the whole card is drawn on.
/// </para>
/// <para>
/// Every value on the card comes from a field <see cref="Monster"/> declares; a pair or a section
/// whose field is empty is not shown at all. Ability modifiers are D&amp;D 5e's own arithmetic
/// (<see cref="AbilityModifier"/>), tinted by their sign.
/// </para>
/// </summary>
public partial class MonsterCardView : UserControl, IEntryCardHeader
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private readonly ImageFrame _portrait;
    private readonly MonsterVitalsView _vitals = new();

    public MonsterCardView()
    {
        InitializeComponent();

        var cell = ThemeResource.Get<double>("DungeonAbilityCellSize");
        _portrait = new ImageFrame
        {
            Width = 3 * cell,
            Height = 4 * cell,
            VerticalAlignment = VerticalAlignment.Top,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconSkull"),
        };

        // The pairs' values start on the title column's line: the label column and the pair's own
        // gap together span the portrait and the gap after it.
        Traits.LabelMinWidth = (3 * cell) + ThemeResource.Get<double>("DungeonDetailColumnGap") - ThemeResource.Get<double>("DungeonSpacingSm");
    }

    public Control HeaderVisual => _portrait;

    public Control HeaderBlock => _vitals;

    public void SetMonster(Monster monster, EntryPicture picture)
    {
        picture.ShowIn(_portrait);
        _vitals.SetMonster(monster);

        PhysicalAbilities.Rows =
        [
            Ability("SIŁ", monster.Str),
            Ability("ZRĘ", monster.Dex),
            Ability("KON", monster.Con),
        ];
        MentalAbilities.Rows =
        [
            Ability("INT", monster.Int),
            Ability("MDR", monster.Wis),
            Ability("CHA", monster.Cha),
        ];

        Traits.Rows = BuildTraitRows(monster);

        Show(SpecialAbilitiesSection, monster.SpecialAbilities);
        Show(ActionsSection, monster.Actions);
        Show(SpellcastingSection, monster.Spellcasting);
        Show(BonusActionsSection, monster.BonusActions);
        Show(ReactionsSection, monster.Reactions);
        Show(LegendaryActionsSection, monster.LegendaryActions);

        Footer.IsVisible = monster.Description is not null;
        DescriptionText.Text = monster.Description;
    }

    /// <summary>
    /// The pairs in statblock order. Zmysły and Wyzwanie are required fields, so they always show;
    /// every other pair only when its field is filled. Wyzwanie is the one value the GM looks for
    /// first, so it is drawn highlighted.
    /// </summary>
    private static IReadOnlyList<TraitRow> BuildTraitRows(Monster monster)
    {
        var rows = new List<TraitRow>();

        AddIfFilled(rows, "Rzuty obronne", monster.SavingThrows);
        AddIfFilled(rows, "Umiejętności", monster.Skills);
        AddIfFilled(rows, "Podatność na obrażenia", monster.DamageVulnerabilities);
        AddIfFilled(rows, "Odporność na obrażenia", monster.DamageResistances);
        AddIfFilled(rows, "Niewrażliwość na obrażenia", monster.DamageImmunities);
        AddIfFilled(rows, "Niewrażliwość na stany", monster.ConditionImmunities);
        rows.Add(new TraitRow("Zmysły", monster.Senses));
        AddIfFilled(rows, "Języki", monster.Languages);
        rows.Add(new TraitRow(
            "Wyzwanie",
            monster.Challenge,
            monster.Xp is { } xp ? $"{xp.ToString("N0", Polish)} PD" : null,
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
