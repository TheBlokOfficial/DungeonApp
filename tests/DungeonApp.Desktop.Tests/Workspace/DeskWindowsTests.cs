using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests.Workspace;

/// <summary>Windows opened and closed while the desk runs: the opening API, closing for good, and what the saved layout brings back.</summary>
public sealed class DeskWindowsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"DungeonApp-desk-windows-{Guid.NewGuid():N}");

    private static WorkspacePanelDescriptor Card(bool closes = true) =>
        new(
            "test.card",
            "Karta",
            "DungeonIconUsers",
            WorkspacePanelGroup.World,
            new PanelPlacement(40, 40, 300, 200),
            new PanelConstraints(200, 120, double.PositiveInfinity, double.PositiveInfinity),
            () => new object())
        {
            AllowsMultipleInstances = true,
            ClosesPermanently = closes,
        };

    [Fact]
    public void Opening_a_window_adds_it_and_opening_it_again_brings_the_same_one_to_the_front()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", []);
        var card = Card();

        var first = desk.Open(card, "one");
        var second = desk.Open(card, "two");
        var again = desk.Open(card, "one");

        Assert.Equal(2, desk.Panels.Count);
        Assert.Same(first, again);
        Assert.True(first.IsActive);
        Assert.False(second.IsActive);
        Assert.Equal(1, first.ZOrder);
    }

    [Fact]
    public void A_singleton_ignores_the_instance_key()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", []);

        Assert.Same(desk.OpenPreview(), desk.OpenPreview());
        Assert.Single(desk.Panels);
    }

    [Fact]
    public void Closing_a_window_removes_it_for_good_and_it_has_no_minimize()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", []);
        var kept = desk.Open(Card(), "kept");
        var closed = desk.Open(Card(), "closed");

        Assert.False(closed.CanMinimize);
        Assert.Contains(closed.HeaderActions, action => action.Command == closed.CloseCommand);

        closed.CloseCommand.Execute(null);

        Assert.Equal([kept], desk.Panels);
        Assert.Empty(desk.MinimizedPanels);
        Assert.True(kept.IsActive);
        Assert.Equal(0, kept.ZOrder);
    }

    [Fact]
    public void A_tool_window_still_minimizes_and_has_no_cross()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", []);
        var tool = desk.Open(Card(closes: false), "tool");

        Assert.True(tool.CanMinimize);
        Assert.Empty(tool.HeaderActions);
    }

    [Fact]
    public async Task The_preview_comes_back_where_it_was_and_a_closed_one_does_not()
    {
        var store = new WorkspaceLayoutStore(_directory);
        var desk = TestDesk.Create(store, "a", []);
        var preview = desk.OpenPreview();
        preview.X = 300;
        preview.Y = 90;
        desk.CommitGesture(preview);
        desk.FlushLayout();
        desk.Dispose();

        var restored = TestDesk.Create(store, "a", [], await store.LoadAsync("a"));
        var window = Assert.Single(restored.Panels);
        Assert.Equal(CampaignWorkspaceViewModel.PreviewDescriptorId, window.Descriptor.Id);
        Assert.Equal(300, window.Desired.X);
        Assert.Equal(90, window.Desired.Y);
        Assert.Equal(PanelDisplayState.Normal, window.State);

        window.CloseCommand.Execute(null);
        restored.FlushLayout();
        restored.Dispose();

        var reopened = TestDesk.Create(store, "a", [], await store.LoadAsync("a"));
        Assert.Empty(reopened.Panels);
    }

    [Fact]
    public void A_pinned_window_of_an_entity_that_no_longer_exists_is_not_restored()
    {
        var layout = new WorkspaceLayout(
            WorkspaceLayout.CurrentVersion,
            1000,
            700,
            [
                new WorkspacePanelLayout(
                    CampaignWorkspaceViewModel.PinnedDescriptorId, Guid.NewGuid().ToString(), true, PanelDisplayState.Normal, 0, 10, 10, 660, 640),
                new WorkspacePanelLayout(
                    CampaignWorkspaceViewModel.PreviewDescriptorId, "frame.world-preview", true, PanelDisplayState.Normal, 1, 20, 20, 660, 640),
            ]);

        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", [], layout);

        Assert.Equal([CampaignWorkspaceViewModel.PreviewDescriptorId], desk.Panels.Select(panel => panel.Descriptor.Id));
    }

    [Fact]
    public void Pinning_an_entity_that_does_not_exist_opens_nothing()
    {
        var desk = TestDesk.Create(new WorkspaceLayoutStore(_directory), "a", []);

        Assert.Null(desk.PinEntity(new DungeonApp.Core.Entries.EntityId(Guid.NewGuid())));
        Assert.Empty(desk.Panels);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
