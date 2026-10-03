using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The monster statblock: portrait, KP / PZ / Szybkość and the ability grid with D&amp;D 5e's own
/// modifier arithmetic (<see cref="AbilityModifier"/>) on top, the filled pairs under it, then the
/// collapsible prose sections. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately
/// after construction, through <see cref="SetMonster"/>: the constructor stays parameterless, as the
/// XAML loader and the designer preview need it.
/// <para>
/// Every value on the card comes from a field <see cref="Monster"/> declares; a pair or a section
/// whose field is empty is not shown at all.
/// </para>
/// </summary>
public partial class MonsterCardView : UserControl
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public MonsterCardView()
    {
        InitializeComponent();
    }

    public void SetMonster(Monster monster, EntryPicture picture)
    {
        picture.ShowIn(Portrait);

        AcValueText.Text = Format(monster.Ac);
        AcNoteText.Text = monster.AcSource;

        HpValueText.Text = Format(monster.Hp);
        HpNoteText.Text = monster.HpDice;

        SpeedValueText.Text = monster.Speed;

        Abilities.Rows =
        [
            new AbilityRow("SIŁ", Format(monster.Str), AbilityModifier.Format(monster.Str)),
            new AbilityRow("ZRĘ", Format(monster.Dex), AbilityModifier.Format(monster.Dex)),
            new AbilityRow("KON", Format(monster.Con), AbilityModifier.Format(monster.Con)),
            new AbilityRow("INT", Format(monster.Int), AbilityModifier.Format(monster.Int)),
            new AbilityRow("MDR", Format(monster.Wis), AbilityModifier.Format(monster.Wis)),
            new AbilityRow("CHA", Format(monster.Cha), AbilityModifier.Format(monster.Cha)),
        ];

        Traits.Rows = BuildTraitRows(monster);

        Show(SpecialAbilitiesSection, SpecialAbilitiesBlock, monster.SpecialAbilities);
        ActionsBlock.Text = monster.Actions;
        Show(SpellcastingSection, SpellcastingBlock, monster.Spellcasting);
        Show(BonusActionsSection, BonusActionsBlock, monster.BonusActions);
        Show(ReactionsSection, ReactionsBlock, monster.Reactions);
        Show(LegendaryActionsSection, LegendaryActionsBlock, monster.LegendaryActions);

        DescriptionSection.IsVisible = monster.Description is not null;
        DescriptionText.Text = monster.Description;
    }

    /// <summary>
    /// The pairs in statblock order. Zmysły and Wyzwanie are required fields, so they always show;
    /// every other pair only when its field is filled.
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
        rows.Add(new TraitRow("Wyzwanie", monster.Challenge, monster.Xp is { } xp ? $"{xp.ToString("N0", Polish)} PD" : null));

        return rows;
    }

    private static void AddIfFilled(List<TraitRow> rows, string label, string? value)
    {
        if (value is not null)
        {
            rows.Add(new TraitRow(label, value));
        }
    }

    private static void Show(Expander section, ProseBlockView block, string? text)
    {
        section.IsVisible = text is not null;
        block.Text = text;
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
