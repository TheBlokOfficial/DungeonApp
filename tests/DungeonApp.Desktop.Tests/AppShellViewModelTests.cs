using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// Every visual build a system's tabs need happens behind the startup curtain (the <c>Warm*Step</c>s
/// under <c>DungeonApp.Desktop.Startup</c>), and the sidebar's collapse state belongs to the frame, not
/// to any one system's sidebar instance. Both are testable here without a window: choosing a system
/// and returning to the selection screen touch no Avalonia control directly, only tab declarations
/// (plain delegates) and <see cref="Shell.Sidebars.GlobalSidebarViewModel"/>.
/// </summary>
public sealed class AppShellViewModelTests
{
    [Fact]
    public async Task Choosing_a_system_builds_no_tab_content_of_its_own()
    {
        var systemTabCalls = 0;
        var campaignTabCalls = 0;
        var systemTabDeclaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", () =>
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
            SystemId.Create("sys-a"), systemTabs: [systemTabDeclaration], campaignTabs: [campaignTabDeclaration]);

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
        var systemA = new FakeGameSystem(SystemId.Create("sys-a"), []);
        var systemB = new FakeGameSystem(SystemId.Create("sys-b"), []);
        var shell = BuildShell([systemA, systemB]);

        await shell.SystemSelection.Systems[0].ChooseCommand.ExecuteAsync();
        Assert.False(shell.Sidebar!.IsCollapsed);

        await shell.Sidebar!.ToggleCollapsedCommand.ExecuteAsync();
        Assert.True(shell.Sidebar!.IsCollapsed);

        await shell.TopBar.ChangeSystemCommand.ExecuteAsync(null);
        Assert.Null(shell.Sidebar);

        await shell.SystemSelection.Systems[1].ChooseCommand.ExecuteAsync();

        Assert.True(shell.Sidebar!.IsCollapsed);
    }

    [Fact]
    public async Task A_freshly_chosen_systems_sidebar_starts_expanded_when_never_collapsed_before()
    {
        var system = new FakeGameSystem(SystemId.Create("sys-a"), []);
        var shell = BuildShell([system]);

        await shell.SystemSelection.Systems[0].ChooseCommand.ExecuteAsync();

        Assert.False(shell.Sidebar!.IsCollapsed);
    }

    private static AppShellViewModel BuildShell(IReadOnlyList<IGameSystem> systems)
    {
        // One shared instance behind every system's id: this fixture is about tab and sidebar
        // wiring, never about which directory a campaign lives in, so every system pointing at the
        // same store is the simplest stand-in.
        ICampaignRepository repository = new InMemoryCampaignRepository();
        var repositoriesBySystem = systems.ToDictionary(system => system.Id, _ => repository);
        var preparations = new CampaignPreparationCache(repositoriesBySystem, systems);
        var campaignLibrary = new CampaignLibraryViewModel(
            repositoriesBySystem, new CreateCampaign(repositoriesBySystem, TimeProvider.System), preparations, systems, _ => Task.CompletedTask);

        return new AppShellViewModel(
            systems,
            repositoriesBySystem,
            campaignLibrary,
            preparations,
            startupSteps: []);
    }
}
