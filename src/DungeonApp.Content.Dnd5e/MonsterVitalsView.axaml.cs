using System.Globalization;
using Avalonia.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The tiles of a monster's armor class, hit points and speed, each with its note (the armor, the
/// hit dice). Built by <see cref="MonsterCardView"/> for the detail header.
/// </summary>
public partial class MonsterVitalsView : UserControl
{
    public MonsterVitalsView()
    {
        InitializeComponent();
    }

    public void SetMonster(Monster monster)
    {
        ArmorClassTile.Value = monster.Ac.ToString(CultureInfo.InvariantCulture);
        ArmorClassTile.Note = monster.AcSource;

        HitPointsTile.Value = monster.Hp.ToString(CultureInfo.InvariantCulture);
        HitPointsTile.Note = monster.HpDice;

        SpeedTile.Value = monster.Speed;
    }
}
