using System;
using System.IO;
using System.Linq;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests.Workspace;

/// <summary>
/// A window whose width has Min == Max (an entry card) changes only in height and has no
/// maximize; an uncapped window whose descriptor opts out of maximizing is treated the same way.
/// </summary>
public sealed class PanelFixedWidthTests : IDisposable
{
    private static readonly PanelConstraints Fixed = new(300, 160, 300, double.PositiveInfinity);
    private static readonly WorkspaceMetrics Metrics = WorkspaceMetrics.Fallback;

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"DungeonApp-fixed-width-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Resizing_any_edge_keeps_the_width_of_a_fixed_width_window()
    {
        var press = new PanelPlacement(100, 100, 300, 400);

        foreach (var edge in new[] { PanelEdge.West, PanelEdge.East, PanelEdge.NorthWest, PanelEdge.SouthEast })
        {
            var result = PanelGeometry.ConstrainResize(
                PanelGeometry.ResizeRaw(press, edge, 80, 50), edge, 1000, 800, Fixed, Metrics);

            Assert.Equal(300, result.Width);
        }
    }

    [Fact]
    public void Resizing_the_bottom_edge_changes_the_height_of_a_fixed_width_window()
    {
        var press = new PanelPlacement(100, 100, 300, 400);

        var result = PanelGeometry.ConstrainResize(
            PanelGeometry.ResizeRaw(press, PanelEdge.South, 0, 120), PanelEdge.South, 1000, 800, Fixed, Metrics);

        Assert.Equal(520, result.Height);
        Assert.Equal(300, result.Width);
    }

    [Fact]
    public void Saved_layout_with_another_width_opens_at_the_fixed_width()
    {
        var layout = new WorkspaceLayout(
            WorkspaceLayout.CurrentVersion,
            1000,
            700,
            [new WorkspacePanelLayout("card", "card", true, PanelDisplayState.Normal, 0, 40, 50, 660, 300)]);
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), Guid.NewGuid().ToString(), [Card], layout);
        desk.SetSurfaceSize(1000, 700);

        var card = desk.Panels.Single();

        Assert.True(card.HasFixedWidth);
        Assert.Equal(300, card.Width);
        Assert.Equal(300, card.Height);
    }

    [Fact]
    public void Window_that_opts_out_of_maximizing_cannot_be_maximized_even_without_a_cap()
    {
        var desk = TestDesk.Create(
            new WorkspaceLayoutStore(_directory),
            Guid.NewGuid().ToString(),
            [Card with { Constraints = new PanelConstraints(300, 160, double.PositiveInfinity, double.PositiveInfinity) }]);
        var panel = desk.Panels.Single();

        Assert.False(panel.HasFixedWidth);
        Assert.False(panel.CanMaximize);
        desk.ToggleMaximize(panel);

        Assert.Equal(PanelDisplayState.Normal, panel.State);
    }

    [Fact]
    public void Fixed_width_window_cannot_be_maximized()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), Guid.NewGuid().ToString(), [Card]);
        var panel = desk.Panels.Single();

        Assert.False(panel.CanMaximize);
        desk.ToggleMaximize(panel);

        Assert.Equal(PanelDisplayState.Normal, panel.State);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static WorkspacePanelDescriptor Card { get; } =
        new("card", "card", "DungeonIconUsers", WorkspacePanelGroup.World, new PanelPlacement(20, 20, 300, 200), Fixed, () => new object())
        {
            CanMaximize = false,
        };
}
