using System;
using System.Threading.Tasks;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Desktop.Tests;

public sealed class WorkspaceLayoutStoreTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"DungeonApp-layout-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_RestoresSavedLayout()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var expectedPanel = new WorkspacePanelLayout(
            "session.clock",
            "clock-1",
            true,
            PanelDisplayState.Normal,
            2,
            10,
            20,
            300,
            240);
        var expected = new WorkspaceLayout(WorkspaceLayout.CurrentVersion, 1032, 724, [expectedPanel]);
        store.Save("campaign-1", expected);

        var actual = await store.LoadAsync("campaign-1");

        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.SurfaceWidth, actual.SurfaceWidth);
        Assert.Equal(expected.SurfaceHeight, actual.SurfaceHeight);
        Assert.Equal(expectedPanel, Assert.Single(actual.Panels));
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_directory))
        {
            System.IO.Directory.Delete(_directory, recursive: true);
        }
    }
}
