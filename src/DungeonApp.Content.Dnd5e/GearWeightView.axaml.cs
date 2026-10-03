using Avalonia.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>An item's weight with its icon, built by <see cref="GearCardView"/> for the detail header.</summary>
public partial class GearWeightView : UserControl
{
    public GearWeightView()
    {
        InitializeComponent();
    }

    public string? Text
    {
        get => WeightText.Text;
        set => WeightText.Text = value;
    }
}
