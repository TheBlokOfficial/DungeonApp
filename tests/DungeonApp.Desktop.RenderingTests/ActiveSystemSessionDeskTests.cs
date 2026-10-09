using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The desk is the campaign position the frame builds itself: once per open campaign, released when
/// the campaign closes. Builds a real desk view, so it runs in the headless window.
/// </summary>
public sealed class ActiveSystemSessionDeskTests
{
    [AvaloniaFact]
    public async Task The_desk_is_built_once_per_open_campaign()
    {
        var session = NewSession();
        session.OpenCampaign(NewCampaign());

        var first = await session.GetOrCreateDeskAsync(() => Task.CompletedTask);
        var second = await session.GetOrCreateDeskAsync(() => Task.CompletedTask);

        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public async Task Closing_the_campaign_releases_the_desk_and_a_new_campaign_gets_a_new_one()
    {
        var session = NewSession();
        session.OpenCampaign(NewCampaign());
        var first = await session.GetOrCreateDeskAsync(() => Task.CompletedTask);

        session.CloseCampaign();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetOrCreateDeskAsync(() => Task.CompletedTask));

        session.OpenCampaign(NewCampaign());
        var second = await session.GetOrCreateDeskAsync(() => Task.CompletedTask);

        Assert.NotSame(first, second);
    }

    [AvaloniaFact]
    public async Task ReleaseAll_releases_the_desk_too()
    {
        var session = NewSession();
        session.OpenCampaign(NewCampaign());
        await session.GetOrCreateDeskAsync(() => Task.CompletedTask);

        session.ReleaseAll();

        Assert.False(session.IsCampaignOpen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetOrCreateDeskAsync(() => Task.CompletedTask));
    }

    [AvaloniaFact]
    public async Task The_desk_offers_the_frames_way_to_close_the_campaign()
    {
        var session = NewSession();
        session.OpenCampaign(NewCampaign());
        var closed = false;

        var desk = await session.GetOrCreateDeskAsync(() =>
        {
            closed = true;
            return Task.CompletedTask;
        });

        var viewModel = Assert.IsType<DungeonApp.Desktop.Workspace.CampaignWorkspaceViewModel>(
            Assert.IsAssignableFrom<Avalonia.Controls.Control>(desk.Content).DataContext);
        await viewModel.CloseCampaign();

        Assert.True(closed);
    }

    private static ActiveSystemSession NewSession() => new(
        new EmptyGameSystem(),
        new InMemoryCampaignRepository(),
        new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-desk-session-tests-{Guid.NewGuid():N}")));

    private static Campaign NewCampaign() => Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System);
}
