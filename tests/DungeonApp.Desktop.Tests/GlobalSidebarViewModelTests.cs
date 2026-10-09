using System.Threading.Tasks;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Shell.Sidebars;
using CommunityToolkit.Mvvm.Input;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The campaign position: the one Kampania-category row that belongs to the frame itself, not to
/// the chosen system. Asserts the row exists straight out of construction, so the shelf always has
/// a position on the sidebar once a system is chosen, and its three state transitions: selected by
/// default, turned into the desk on open, and back to the shelf (still selected) on close.
/// </summary>
public sealed class GlobalSidebarViewModelTests
{
    [Fact]
    public void The_campaign_position_exists_and_is_selected_right_after_a_system_is_chosen()
    {
        var sidebar = BuildSidebar();

        Assert.NotNull(sidebar.CampaignPositionItem);
        Assert.True(sidebar.CampaignPositionItem.IsActive);
        Assert.False(sidebar.CampaignPositionItem.IsLocked);
    }

    [Fact]
    public void Opening_a_campaign_turns_the_campaign_position_into_the_desk()
    {
        var sidebar = BuildSidebar();
        var shelfIcon = sidebar.CampaignPositionItem.IconResourceKey;

        sidebar.SetCampaignOpen(true);

        Assert.Equal("Biurko", sidebar.CampaignPositionItem.Label);
        Assert.Equal("DungeonIconDockBottom", sidebar.CampaignPositionItem.IconResourceKey);
        Assert.NotEqual(shelfIcon, sidebar.CampaignPositionItem.IconResourceKey);
        Assert.Single(sidebar.CampaignItems);
    }

    [Fact]
    public void Closing_the_campaign_restores_the_shelf_label_and_keeps_the_position_selected()
    {
        var sidebar = BuildSidebar();
        var shelfIcon = sidebar.CampaignPositionItem.IconResourceKey;
        sidebar.SetCampaignOpen(true);
        sidebar.ActivateCampaignPosition();

        sidebar.SetCampaignOpen(false);
        sidebar.ActivateCampaignPosition();

        Assert.Equal("Kampanie", sidebar.CampaignPositionItem.Label);
        Assert.Equal(shelfIcon, sidebar.CampaignPositionItem.IconResourceKey);
        Assert.True(sidebar.CampaignPositionItem.IsActive);
    }

    [Fact]
    public async Task Activating_the_campaign_position_after_a_different_row_was_selected_makes_it_the_only_active_row()
    {
        var declaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", () => new FakeTabContent());
        var sidebar = BuildSidebar(systemTabs: [declaration]);
        await ((IAsyncRelayCommand)sidebar.SystemTabItems[0].SelectCommand).ExecuteAsync(null);
        Assert.False(sidebar.CampaignPositionItem.IsActive);

        sidebar.ActivateCampaignPosition();

        Assert.True(sidebar.CampaignPositionItem.IsActive);
        Assert.False(sidebar.SystemTabItems[0].IsActive);
    }

    [Fact]
    public async Task Selecting_the_campaign_position_invokes_the_shells_callback()
    {
        var calls = 0;
        var sidebar = new GlobalSidebarViewModel(
            [],
            [],
            () =>
            {
                calls++;
                return Task.CompletedTask;
            },
            _ => Task.CompletedTask,
            _ => { },
            () => Task.CompletedTask,
            () => Task.CompletedTask);

        await ((IAsyncRelayCommand)sidebar.CampaignPositionItem.SelectCommand).ExecuteAsync(null);

        Assert.Equal(1, calls);
    }

    private static GlobalSidebarViewModel BuildSidebar(
        System.Collections.Generic.IReadOnlyList<SystemTabDeclaration>? systemTabs = null,
        System.Collections.Generic.IReadOnlyList<CampaignTabDeclaration>? campaignTabs = null) =>
        new(
            systemTabs ?? [],
            campaignTabs ?? [],
            () => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => { },
            () => Task.CompletedTask,
            () => Task.CompletedTask);
}
