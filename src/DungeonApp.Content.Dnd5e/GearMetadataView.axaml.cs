using Avalonia.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// An item's weight with its icon and its worth under it, built by <see cref="GearCardView"/> for
/// the detail header - every item has both.
/// </summary>
public partial class GearMetadataView : UserControl
{
    public GearMetadataView()
    {
        InitializeComponent();
    }

    /// <summary>The weight in its scale ("1,5 kg") and the worth as it reads ("wartość 75").</summary>
    public void Show(string weight, string worth)
    {
        WeightText.Text = weight;
        WorthText.Text = worth;
    }
}
