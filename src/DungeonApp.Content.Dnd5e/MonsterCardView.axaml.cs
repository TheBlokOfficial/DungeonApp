using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The monster statblock: stat-strip, ability grid with D&amp;D 5e's own modifier arithmetic
/// (<see cref="AbilityModifier"/>), skills-and-senses, wyzwanie as a chip, cechy szczególne / akcje
/// under bar-accented headers, and an italic description.
/// <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately after construction, through
/// <see cref="SetMonster"/> - see <see cref="Gear"/>'s card for the same reasoning on why this stays
/// a plain parameterless constructor.
/// <para>
/// The KP and PZ stats carry a bracketed note ("skórz., tarcza", "2k6") taken from
/// <see cref="Monster.AcSource"/> and <see cref="Monster.HpDice"/>. Every value on the card comes
/// from a field <see cref="Monster"/> declares.
/// </para>
/// </summary>
public partial class MonsterCardView : UserControl
{
    public MonsterCardView()
    {
        InitializeComponent();
    }

    public void SetMonster(Monster monster)
    {
        AcValueRun.Text = Format(monster.Ac);
        AcNoteRun.Text = Note(monster.AcSource);

        HpValueRun.Text = Format(monster.Hp);
        HpNoteRun.Text = Note(monster.HpDice);

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

        SkillsAndSenses.Rows = BuildSkillsAndSensesRows(monster);
        ChallengeText.Text = monster.Challenge;

        SpecialAbilitiesSection.IsVisible = monster.SpecialAbilities is not null;
        SpecialAbilitiesBlock.Text = monster.SpecialAbilities;

        ActionsBlock.Text = monster.Actions;

        DescriptionSection.IsVisible = monster.Description is not null;
        DescriptionBlock.Text = monster.Description;
    }

    /// <summary>Umiejętności (optional) and Języki (optional) around Zmysły (required) - Wyzwanie is its own chip row, not a plain trait row.</summary>
    private static IReadOnlyList<TraitRow> BuildSkillsAndSensesRows(Monster monster)
    {
        var rows = new List<TraitRow>();

        if (monster.Skills is not null)
        {
            rows.Add(new TraitRow("Umiejętności", monster.Skills));
        }

        rows.Add(new TraitRow("Zmysły", monster.Senses));

        if (monster.Languages is not null)
        {
            rows.Add(new TraitRow("Języki", monster.Languages));
        }

        return rows;
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The "(skórz., tarcza)" / "(2k6)" bracketed note - empty (not null) when the source field is absent, so the inline Run simply prints nothing.</summary>
    private static string Note(string? source) => source is null ? string.Empty : $" ({source})";
}
