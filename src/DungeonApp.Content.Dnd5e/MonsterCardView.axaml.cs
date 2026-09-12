using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Desktop.Controls.Content;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The monster statblock, composed from <see cref="TraitListView"/> and <see cref="ProseBlockView"/>.
/// <see cref="Dnd5eContentSet.CreateCard"/> sets the model once, immediately after construction,
/// through <see cref="SetMonster"/> - there is no bindable property to update it later, because
/// nothing in this application ever needs to: a card is built fresh every time an entry is selected.
/// A plain parameterless constructor (rather than taking the model as a constructor argument) keeps
/// this control instantiable the same way every other view in the shell is, including by Avalonia's
/// own design-time tooling.
/// <para>
/// A row whose optional field the entry left blank is left out entirely, matching the layout
/// docs/architecture.md's example fixture describes: no label ever stands next to nothing. This is
/// ordinary presentation logic over already-typed, already-validated values - not the kind of
/// branching CLAUDE.md's five prohibitions rule out, which govern game-mechanical computation, not
/// whether a UI element has something to show.
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
        SizeTypeAlignment.Rows =
        [
            new TraitRow("Rozmiar", monster.Size),
            new TraitRow("Typ", monster.Type),
            new TraitRow("Charakter", monster.Alignment)
        ];

        DefenseAndSpeed.Rows =
        [
            new TraitRow("KP", Format(monster.Ac), monster.AcSource),
            new TraitRow("PZ", Format(monster.Hp), monster.HpDice),
            new TraitRow("Szybkość", monster.Speed)
        ];

        Abilities.Rows =
        [
            new TraitRow("SIŁ", Format(monster.Str)),
            new TraitRow("ZRĘ", Format(monster.Dex)),
            new TraitRow("KON", Format(monster.Con)),
            new TraitRow("INT", Format(monster.Int)),
            new TraitRow("MDR", Format(monster.Wis)),
            new TraitRow("CHA", Format(monster.Cha))
        ];

        SkillsAndSenses.Rows = BuildSkillsAndSensesRows(monster);

        SpecialAbilitiesBlock.IsVisible = monster.SpecialAbilities is not null;
        SpecialAbilitiesBlock.Text = monster.SpecialAbilities;

        ActionsBlock.Text = monster.Actions;

        DescriptionBlock.IsVisible = monster.Description is not null;
        DescriptionBlock.Text = monster.Description;
    }

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

        rows.Add(new TraitRow("Wyzwanie", monster.Challenge));

        return rows;
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
