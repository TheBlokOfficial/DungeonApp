using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests.Workspace;

/// <summary>
/// Maximizing is a session-only state of panels without a size cap: capped panels cannot do it, and
/// neither saving nor loading a layout carries it over.
/// </summary>
public sealed class PanelMaximizeTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DungeonApp-maximize-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Panel_with_a_size_cap_cannot_be_maximized()
    {
        var (desk, _, _) = CreateDesk(Capped, Open);
        var capped = desk.Panels.Single(panel => panel.Descriptor.Id == "capped");

        Assert.False(capped.CanMaximize);
        desk.ToggleMaximize(capped);

        Assert.Equal(PanelDisplayState.Normal, capped.State);
    }

    [Fact]
    public void Panel_without_a_cap_in_either_axis_can_be_maximized_and_restored()
    {
        var (desk, _, _) = CreateDesk(Capped, Open);
        var open = desk.Panels.Single(panel => panel.Descriptor.Id == "open");
        desk.SetSurfaceSize(1000, 700);

        Assert.True(open.CanMaximize);
        desk.ToggleMaximize(open);

        Assert.Equal(PanelDisplayState.Maximized, open.State);
        Assert.Equal((0d, 0d, 1000d, 700d), (open.X, open.Y, open.Width, open.Height));

        desk.ToggleMaximize(open);

        Assert.Equal(PanelDisplayState.Normal, open.State);
        Assert.Equal(open.Desired.Width, open.Width);
    }

    [Fact]
    public void A_cap_in_one_axis_only_still_blocks_maximizing()
    {
        var (desk, _, _) = CreateDesk(
            Descriptor("wide", new PanelConstraints(240, 160, double.PositiveInfinity, 600)));

        Assert.False(desk.Panels[0].CanMaximize);
    }

    [Fact]
    public void Layout_saved_with_a_maximized_panel_stores_it_as_normal_with_its_desired_geometry()
    {
        var (desk, store, workspaceId) = CreateDesk(Open);
        var open = desk.Panels[0];
        desk.SetSurfaceSize(1000, 700);
        desk.ToggleMaximize(open);

        desk.FlushLayout();

        var saved = store.Load(workspaceId).Panels.Single();
        Assert.Equal(PanelDisplayState.Normal, saved.State);
        Assert.Equal(open.Desired.X, saved.X);
        Assert.Equal(open.Desired.Width, saved.Width);
        Assert.NotEqual(1000d, saved.Width);
    }

    [Fact]
    public void Layout_read_with_a_maximized_panel_opens_it_as_normal()
    {
        var layout = new WorkspaceLayout(
            WorkspaceLayout.CurrentVersion,
            1000,
            700,
            [new WorkspacePanelLayout("open", "open", true, PanelDisplayState.Maximized, 0, 40, 50, 300, 200)]);
        var store = new WorkspaceLayoutStore(_directory);

        var desk = TestDesk.Create(store, Guid.NewGuid().ToString(), [Open], layout);

        Assert.Equal(PanelDisplayState.Normal, desk.Panels.Single().State);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static WorkspacePanelDescriptor Capped { get; } =
        Descriptor("capped", new PanelConstraints(240, 160, 384, 256));

    private static WorkspacePanelDescriptor Open { get; } =
        Descriptor("open", new PanelConstraints(240, 160, double.PositiveInfinity, double.PositiveInfinity));

    private static WorkspacePanelDescriptor Descriptor(string id, PanelConstraints constraints) =>
        new(id, id, "DungeonIconUsers", WorkspacePanelGroup.World, new PanelPlacement(20, 20, 300, 200), constraints, () => new object());

    private (CampaignWorkspaceViewModel Desk, WorkspaceLayoutStore Store, string WorkspaceId) CreateDesk(
        params WorkspacePanelDescriptor[] descriptors)
    {
        var store = new WorkspaceLayoutStore(_directory);
        var workspaceId = Guid.NewGuid().ToString();
        return (TestDesk.Create(store, workspaceId, descriptors), store, workspaceId);
    }
}
