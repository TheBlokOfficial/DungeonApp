using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public partial class CampaignLibraryView : UserControl
{
    public CampaignLibraryView()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.FocusManager.GetFocusedElement() is not TextBox focusedTextBox)
        {
            return;
        }

        // Keep editing when the press is still inside the focused field. Any press elsewhere clears
        // the edit focus before its actual target handles the event; the event remains unhandled, so
        // a button or another input can then receive focus and act normally.
        if (e.Source is Avalonia.Visual source &&
            (ReferenceEquals(source, focusedTextBox) ||
             ReferenceEquals(source.FindAncestorOfType<TextBox>(), focusedTextBox)))
        {
            return;
        }

        // Avalonia 12.0.5 clears focus by focusing a null element. The dedicated ClearFocus API is
        // newer; keep this call explicit until the framework upgrade makes that API available.
        topLevel.FocusManager.Focus(null, NavigationMethod.Pointer, e.KeyModifiers);
    }
}
