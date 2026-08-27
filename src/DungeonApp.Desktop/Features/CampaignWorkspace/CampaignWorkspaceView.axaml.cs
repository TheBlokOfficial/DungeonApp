using Avalonia.Controls;
using Avalonia.Interactivity;
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

        // Bubbles up from the panel that was dragged or resized.
        AddHandler(PanelWindow.GestureCompletedEvent, OnGestureCompleted);
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
