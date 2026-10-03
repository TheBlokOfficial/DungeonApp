using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The gear card. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately after
/// construction, through <see cref="SetGear"/>: the constructor stays parameterless, as the XAML
/// loader and the designer preview need it.
/// <para>
/// The square picture - three ability cells wide, like the monster's portrait, so every card's
/// title starts on the same line - and the property pairs are lent to the detail header
/// (<see cref="IEntryCardHeader"/>); a gear item with no property lends no block.
/// </para>
/// </summary>
public partial class GearCardView : UserControl, IEntryCardHeader
{
    private readonly ImageFrame _picture;
    private TraitListView? _properties;

    public GearCardView()
    {
        InitializeComponent();

        var side = 3 * ThemeResource.Get<double>("DungeonAbilityCellSize");
        _picture = new ImageFrame
        {
            Width = side,
            Height = side,
            VerticalAlignment = VerticalAlignment.Top,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconBackpack"),
        };
    }

    public Control HeaderVisual => _picture;

    public Control? HeaderBlock => _properties;

    public void SetGear(Gear gear, EntryPicture picture)
    {
        picture.ShowIn(_picture);

        // No rarity row: rarity is this content type's only badge, already shown in the list, and
        // the card does not repeat it.
        var rows = new List<TraitRow>();

        if (gear.Weight is { } weight)
        {
            rows.Add(new TraitRow("Waga", weight.ToString(CultureInfo.InvariantCulture)));
        }

        _properties = rows.Count > 0 ? new TraitListView { Rows = rows } : null;

        DescriptionBlock.IsVisible = gear.Description is not null;
        DescriptionBlock.Text = gear.Description;
    }
}
