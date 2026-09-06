using DungeonApp.Desktop.Controls.Workspace;

namespace DungeonApp.Desktop.Tests;

public sealed class PanelGeometryTests
{
    [Fact]
    public void FitInto_ClampsRestoredPlacementToMaximumSize()
    {
        var desired = new PanelPlacement(40, 48, 900, 700);
        var constraints = new PanelConstraints(240, 152, 384, 256);
        var metrics = new WorkspaceMetrics(0, 16, 240, 152);

        var actual = PanelGeometry.FitInto(desired, 1600, 1000, constraints, metrics);

        Assert.Equal(384, actual.Width);
        Assert.Equal(256, actual.Height);
        Assert.Equal(40, actual.X);
        Assert.Equal(48, actual.Y);
    }

    [Fact]
    public void FitInto_ClampsSmallPlacementToMinimumSize()
    {
        var desired = new PanelPlacement(40, 48, 120, 80);
        var constraints = new PanelConstraints(256, 192, 384, 256);
        var metrics = new WorkspaceMetrics(0, 16, 240, 152);

        var actual = PanelGeometry.FitInto(desired, 1600, 1000, constraints, metrics);

        Assert.Equal(256, actual.Width);
        Assert.Equal(192, actual.Height);
    }
}
