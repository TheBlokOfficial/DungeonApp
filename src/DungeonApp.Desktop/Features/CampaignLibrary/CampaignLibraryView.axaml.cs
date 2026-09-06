using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

public partial class CampaignLibraryView : UserControl
{
    public CampaignLibraryView()
    {
        InitializeComponent();
        AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Extent and viewport are available only after the first layout pass.
        Dispatcher.UIThread.Post(UpdateCampaignListFades, DispatcherPriority.Loaded);
    }

    private void OnCampaignListScrollChanged(object? sender, ScrollChangedEventArgs e)
        => UpdateCampaignListFades();

    private void UpdateCampaignListFades()
    {
        const double edgeTolerance = 0.5;
        var verticalRange = Math.Max(0, CampaignList.Extent.Height - CampaignList.Viewport.Height);
        var hasOverflow = verticalRange > edgeTolerance;

        CampaignListTopFade.IsVisible = hasOverflow && CampaignList.Offset.Y > edgeTolerance;
        CampaignListBottomFade.IsVisible = hasOverflow && CampaignList.Offset.Y < verticalRange - edgeTolerance;
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
