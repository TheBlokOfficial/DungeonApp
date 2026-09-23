using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The tab lifecycle on a substituted system (docs/tasks.md, "Testy"): closed without a campaign,
/// open after one is opened, content built once and released at close, at "powrót" (here,
/// <see cref="ActiveSystemSession.ReleaseAll"/>) and after warmup - the four release triggers
/// docs/architecture.md's navigation section names.
/// </summary>
public sealed class ActiveSystemSessionTests
{
    private static readonly SystemId FakeSystemId = SystemId.Create("fake-system");

    [Fact]
    public async Task Campaign_tabs_are_unreachable_before_a_campaign_is_open()
    {
        var declaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ => Task.FromResult<ITabContent>(new FakeTabContent()));
        var session = BuildSession(campaignTabs: [declaration]);

        Assert.False(session.IsCampaignOpen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetOrCreateCampaignTabAsync(declaration));
    }

    [Fact]
    public void Opening_a_campaign_makes_it_reported_as_open()
    {
        var session = BuildSession();

        session.OpenCampaign(NewCampaign());

        Assert.True(session.IsCampaignOpen);
    }

    [Fact]
    public async Task A_campaign_tabs_content_is_built_once_and_the_same_instance_is_returned_on_later_shows()
    {
        var calls = 0;
        var declaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ =>
        {
            calls++;
            return Task.FromResult<ITabContent>(new FakeTabContent());
        });
        var session = BuildSession(campaignTabs: [declaration]);
        session.OpenCampaign(NewCampaign());

        var first = await session.GetOrCreateCampaignTabAsync(declaration);
        var second = await session.GetOrCreateCampaignTabAsync(declaration);

        Assert.Same(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_system_tabs_content_is_built_once_and_the_same_instance_is_returned_on_later_shows()
    {
        var calls = 0;
        var declaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", () =>
        {
            calls++;
            return new FakeTabContent();
        });
        var session = BuildSession(systemTabs: [declaration]);

        var first = session.GetOrCreateSystemTab(declaration);
        var second = session.GetOrCreateSystemTab(declaration);

        Assert.Same(first, second);
        Assert.Equal(1, calls);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Closing_the_campaign_disposes_every_campaign_tab_it_built_and_locks_them_again()
    {
        var declaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ => Task.FromResult<ITabContent>(new FakeTabContent()));
        var session = BuildSession(campaignTabs: [declaration]);
        session.OpenCampaign(NewCampaign());
        var content = (FakeTabContent)await session.GetOrCreateCampaignTabAsync(declaration);

        session.CloseCampaign();

        Assert.True(content.IsDisposed);
        Assert.False(session.IsCampaignOpen);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GetOrCreateCampaignTabAsync(declaration));
    }

    [Fact]
    public async Task ReleaseAll_disposes_every_system_tab_and_every_campaign_tab_and_closes_the_campaign()
    {
        var systemContent = new FakeTabContent();
        var campaignContent = new FakeTabContent();
        var systemDeclaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", () => systemContent);
        var campaignDeclaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ => Task.FromResult<ITabContent>(campaignContent));
        var session = BuildSession(systemTabs: [systemDeclaration], campaignTabs: [campaignDeclaration]);
        session.OpenCampaign(NewCampaign());
        session.GetOrCreateSystemTab(systemDeclaration);
        await session.GetOrCreateCampaignTabAsync(campaignDeclaration);

        session.ReleaseAll();

        Assert.True(systemContent.IsDisposed);
        Assert.True(campaignContent.IsDisposed);
        Assert.False(session.IsCampaignOpen);
    }

    /// <summary>
    /// Calling <see cref="ActiveSystemSession.ReleaseAll"/> is how "powrót do wyboru" and "wyjście z
    /// programu" both release tabs (docs/architecture.md). Calling it twice - once per shutdown hook,
    /// the same way the previous shell's <c>FlushPendingState</c> could run from two hooks - must not
    /// double-dispose anything.
    /// </summary>
    [Fact]
    public async Task ReleaseAll_is_safe_to_call_more_than_once()
    {
        var content = new FakeTabContent();
        var declaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ => Task.FromResult<ITabContent>(content));
        var session = BuildSession(campaignTabs: [declaration]);
        session.OpenCampaign(NewCampaign());
        await session.GetOrCreateCampaignTabAsync(declaration);

        session.ReleaseAll();
        var exception = Record.Exception(session.ReleaseAll);

        Assert.Null(exception);
    }

    /// <summary>
    /// Mimics <c>AppShellViewModel.WarmFirstCampaignAsync</c>: warmup builds and disposes its own
    /// content against a throwaway session, entirely outside <see cref="ActiveSystemSession"/>'s
    /// cache. A later real open of the same campaign must still build fresh content - "zawartość
    /// zakładki... zwalniana... po rozgrzewce" (docs/tasks.md) means warmup's instance never leaks
    /// into the real one.
    /// </summary>
    [Fact]
    public async Task Content_built_and_released_by_warmup_does_not_leak_into_a_later_real_open()
    {
        var calls = 0;
        var declaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ =>
        {
            calls++;
            return Task.FromResult<ITabContent>(new FakeTabContent());
        });
        var repository = new InMemoryCampaignRepository();
        var campaign = NewCampaign();
        await repository.SaveAsync(campaign, []);

        var warmupSession = new CampaignSession(campaign, repository, []);
        var warmupContext = new CampaignTabContext(warmupSession);
        var warmedUp = (FakeTabContent)await declaration.CreateContentAsync(warmupContext);
        warmedUp.Dispose();

        var system = new FakeGameSystem(FakeSystemId, [], campaignTabs: [declaration]);
        var session = new ActiveSystemSession(system, repository);
        session.OpenCampaign(campaign);
        var real = await session.GetOrCreateCampaignTabAsync(declaration);

        Assert.Equal(2, calls);
        Assert.NotSame(warmedUp, real);
        Assert.False(((FakeTabContent)real).IsDisposed);
    }

    private static ActiveSystemSession BuildSession(
        System.Collections.Generic.IReadOnlyList<SystemTabDeclaration>? systemTabs = null,
        System.Collections.Generic.IReadOnlyList<CampaignTabDeclaration>? campaignTabs = null)
    {
        var system = new FakeGameSystem(FakeSystemId, systemTabs: systemTabs, campaignTabs: campaignTabs);
        return new ActiveSystemSession(system, new InMemoryCampaignRepository());
    }

    private static Campaign NewCampaign() => Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System);
}
