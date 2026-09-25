using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public partial class CampaignLibraryView : UserControl
{
    public CampaignLibraryView()
    {
        InitializeComponent();
    }

    private static void OnDeletePointerEntered(object? sender, PointerEventArgs e)
    {
        SetDeleteHoverState(sender, isHovered: true);
    }

    private static void OnDeletePointerExited(object? sender, PointerEventArgs e)
    {
        SetDeleteHoverState(sender, isHovered: false);
    }

    private static void SetDeleteHoverState(object? sender, bool isHovered)
    {
        if (sender is not Control deleteButton)
        {
            return;
        }

        var row = deleteButton.FindAncestorOfType<Border>();
        if (row is not null)
        {
            row.Classes.Set("delete-hover", isHovered);
        }
    }

}
