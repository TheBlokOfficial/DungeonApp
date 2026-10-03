using Avalonia.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// An item's weight with its icon and its worth under it, built by <see cref="GearCardView"/> for
/// the detail header when the item has either.
/// </summary>
public partial class GearMetadataView : UserControl
{
    public GearMetadataView()
    {
        InitializeComponent();
    }

    /// <summary>The weight in its scale, and the worth as it reads ("wartość 75"); either may be null, not both.</summary>
    public void Show(string? weight, string? worth)
    {
        Weight.IsVisible = weight is not null;
        WeightText.Text = weight;

        // Without a weight the worth moves up into the first line, where the weight would stand.
        FirstLineWorthText.IsVisible = weight is null;
        FirstLineWorthText.Text = weight is null ? worth : null;
        WorthText.IsVisible = weight is not null && worth is not null;
        WorthText.Text = weight is not null ? worth : null;
    }
}
