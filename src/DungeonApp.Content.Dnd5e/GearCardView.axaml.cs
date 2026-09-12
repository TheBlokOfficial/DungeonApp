using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Desktop.Controls.Content;

namespace DungeonApp.Content.Dnd5e;

/// <summary>The gear card. See <see cref="MonsterCardView"/>'s remarks - the same reasoning applies.</summary>
public partial class GearCardView : UserControl
{
    public GearCardView()
    {
        InitializeComponent();
    }

    public void SetGear(Gear gear)
    {
        var rows = new List<TraitRow> { new("Rzadkość", gear.Rarity) };

        if (gear.Weight is { } weight)
        {
            rows.Add(new TraitRow("Waga", weight.ToString(CultureInfo.InvariantCulture)));
        }

        Properties.Rows = rows;

        DescriptionBlock.IsVisible = gear.Description is not null;
        DescriptionBlock.Text = gear.Description;
    }
}
