using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace;

/// <summary>
/// The only code-behind in the project that does anything, and deliberately so: pointer gestures and
/// reacting to the container's size are view concerns. There are no use case calls here.
/// </summary>
public partial class CampaignWorkspaceView : UserControl
{
    /// <summary>
    /// Below this, the reported size is a transient from the first layout passes or from the window
    /// being minimized to the taskbar. Re-fitting against those would collapse every panel into the
    /// corner and destroy a freshly restored arrangement.
    /// </summary>
    private const double MinimumUsableSurface = 200;

    public CampaignWorkspaceView()
    {
        InitializeComponent();

        Surface.SizeChanged += OnSurfaceSizeChanged;
        Surface.AddHandler(InputElement.PointerPressedEvent, OnSurfacePointerPressed, RoutingStrategies.Tunnel);

        // Bubbles up from the panel that was dragged or resized.
        AddHandler(PanelWindow.GestureCompletedEvent, OnGestureCompleted);
    }

    private void OnSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(Surface).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // The surface sees tunneled presses before a child can mark them handled. Only the actual
        // desk clears selection; presses anywhere inside panel chrome or content leave activation to
        // PanelWindow, and the taskbar is a sibling outside this event route.
        if (e.Source is Avalonia.Visual source &&
            (source is PanelWindow || source.FindAncestorOfType<PanelWindow>() is not null))
        {
            return;
        }

        if (DataContext is CampaignWorkspaceViewModel viewModel)
        {
            viewModel.ClearActivePanel();
        }
    }

    private void OnSurfaceSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is not CampaignWorkspaceViewModel viewModel)
        {
            return;
        }

        if (e.NewSize.Width < MinimumUsableSurface || e.NewSize.Height < MinimumUsableSurface)
        {
            return;
        }

        // No "layout restored yet?" guard is needed: the view model restores its arrangement
        // synchronously in its constructor, so by the time any size is reported it already holds the
        // desired placements.
        viewModel.SetSurfaceSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnGestureCompleted(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CampaignWorkspaceViewModel viewModel &&
            e.Source is PanelWindow { DataContext: WorkspacePanelViewModel panel })
        {
            viewModel.CommitGesture(panel);
        }
    }
}
