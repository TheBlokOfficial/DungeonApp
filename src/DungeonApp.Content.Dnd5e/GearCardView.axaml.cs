using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using DungeonApp.Desktop.Entries.Controls;

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
        // No rarity row: rarity is this content type's only tag, already shown once in the detail
        // header, and the card does not repeat what the header shows.
        var rows = new List<TraitRow>();

        if (gear.Weight is { } weight)
        {
            rows.Add(new TraitRow("Waga", weight.ToString(CultureInfo.InvariantCulture)));
        }

        Properties.Rows = rows;

        DescriptionBlock.IsVisible = gear.Description is not null;
        DescriptionBlock.Text = gear.Description;
    }
}
