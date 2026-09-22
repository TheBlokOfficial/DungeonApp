using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// docs/tasks.md's revised zadanie 1 moved every visual build a system's tabs need behind the startup
/// curtain (see the new <c>Warm*Step</c>s under <c>DungeonApp.Desktop.Startup</c>); zadanie 3 makes the
/// sidebar's collapse state belong to the frame, not to any one system's sidebar instance. Both are
/// testable here without a window: <see cref="AppShellViewModel.ChooseSystemAsync"/> and
/// <see cref="AppShellViewModel.ReturnToSelectionAsync"/> touch no Avalonia control directly, only
/// tab declarations (plain delegates) and <see cref="Shell.Sidebars.GlobalSidebarViewModel"/>.
/// </summary>
public sealed class AppShellViewModelTests
{
    [Fact]
    public async Task Choosing_a_system_builds_no_tab_content_of_its_own()
    {
        var systemTabCalls = 0;
        var campaignTabCalls = 0;
        var systemTabDeclaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", _ =>
        {
            systemTabCalls++;
            return new FakeTabContent();
        });
        var campaignTabDeclaration = new CampaignTabDeclaration("camp.tab", "Tab", "icon", _ =>
        {
            campaignTabCalls++;
            return Task.FromResult<ITabContent>(new FakeTabContent());
        });
        var system = new FakeGameSystem(
            ContentId.Create("sys-a"), [], systemTabs: [systemTabDeclaration], campaignTabs: [campaignTabDeclaration]);

        var shell = BuildShell([system]);

        await shell.SystemSelection.Systems[0].ChooseCommand.ExecuteAsync();

        Assert.Equal(0, systemTabCalls);
        Assert.Equal(0, campaignTabCalls);
        Assert.True(shell.IsSystemChosen);
        Assert.NotNull(shell.Sidebar);
    }

    [Fact]
    public async Task Collapsing_the_sidebar_then_changing_the_system_keeps_the_next_sidebar_collapsed()
    {
        var systemA = new FakeGameSystem(ContentId.Create("sys-a"), []);
        var systemB = new FakeGameSystem(ContentId.Create("sys-b"), []);
        var shell = BuildShell([systemA, systemB]);

        await shell.SystemSelection.Systems[0].ChooseCommand.ExecuteAsync();
        Assert.False(shell.Sidebar!.IsCollapsed);

        await shell.Sidebar!.ToggleCollapsedCommand.ExecuteAsync();
        Assert.True(shell.Sidebar!.IsCollapsed);

        await ((AsyncCommand)shell.Sidebar!.ChangeSystemItem.SelectCommand).ExecuteAsync();
        Assert.Null(shell.Sidebar);

        await shell.SystemSelection.Systems[1].ChooseCommand.ExecuteAsync();

        Assert.True(shell.Sidebar!.IsCollapsed);
    }

    [Fact]
    public async Task A_freshly_chosen_systems_sidebar_starts_expanded_when_never_collapsed_before()
    {
        var system = new FakeGameSystem(ContentId.Create("sys-a"), []);
        var shell = BuildShell([system]);

        await shell.SystemSelection.Systems[0].ChooseCommand.ExecuteAsync();

        Assert.False(shell.Sidebar!.IsCollapsed);
    }

    private static AppShellViewModel BuildShell(System.Collections.Generic.IReadOnlyList<IGameSystem> systems)
    {
        var repository = new InMemoryCampaignRepository();
        var campaignLibrary = new CampaignLibraryViewModel(
            repository, new CreateCampaign(repository, TimeProvider.System, []), _ => Task.CompletedTask);
        var preparations = new CampaignPreparationCache(repository, []);

        return new AppShellViewModel(
            systems,
            repository,
            campaignLibrary,
            preparations,
            startupSteps: [],
            contentRegistry: () => new ContentRegistry([], [], [], []));
    }
}
