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
        // krok 10, brief A10: "Karta bez dublujących wierszy" - rarity is already this content
        // type's only tag, shown once in the detail header. No row for it here any more.
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
