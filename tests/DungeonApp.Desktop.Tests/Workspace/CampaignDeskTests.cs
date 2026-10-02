using System;
using System.IO;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests.Workspace;

/// <summary>
/// A desk created and released without a gesture writes no layout. Restoring a layout and fitting
/// it to a surface must never mark it dirty on their own - only a gesture
/// (<see cref="CampaignWorkspaceViewModel.CommitGesture"/>) does that.
/// <para>
/// Exercises <see cref="CampaignWorkspaceViewModel"/> directly rather than through
/// <see cref="CampaignDesk.CreateAsync"/>: the latter builds a real <c>CampaignWorkspaceView</c>,
/// whose compiled XAML needs a running Avalonia application this test project has no headless
/// harness for. <c>CampaignDesk</c>'s own release sequence - flush, then dispose - is reproduced by
/// hand below, in that same order.
/// </para>
/// </summary>
public sealed class CampaignDeskTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DungeonApp-desk-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Creating_and_releasing_a_desk_without_a_gesture_writes_no_layout_file()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var workspaceId = Guid.NewGuid().ToString();

        var viewModel = new CampaignWorkspaceViewModel(store, workspaceId, WorkspaceLayout.Empty, tools: []);
        // The same order CampaignDesk's ITabContent.Dispose uses: flush whatever is pending, then
        // release the view model's own subscriptions.
        viewModel.FlushLayout();
        viewModel.Dispose();

        Assert.False(File.Exists(LayoutPath(workspaceId)));
    }

    [Fact]
    public void Releasing_a_desk_after_a_gesture_flushes_the_pending_layout()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var workspaceId = Guid.NewGuid().ToString();
        var descriptor = new WorkspacePanelDescriptor(
            "fake.tool",
            "Fake",
            "DungeonIconUsers",
            WorkspacePanelGroup.World,
            new PanelPlacement(0, 0, 240, 160),
            new PanelConstraints(240, 160, 800, 600),
            () => new object());

        var viewModel = new CampaignWorkspaceViewModel(store, workspaceId, WorkspaceLayout.Empty, [descriptor]);
        viewModel.CommitGesture(viewModel.Panels[0]);

        // The desk's debounce timer never fires on its own here - no dispatcher runs in a unit test -
        // so a file existing afterwards can only be explained by FlushLayout's own immediate write.
        viewModel.FlushLayout();
        viewModel.Dispose();

        Assert.True(File.Exists(LayoutPath(workspaceId)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string LayoutPath(string workspaceId) => Path.Combine(_directory, "layouts", $"{workspaceId}.json");
}
