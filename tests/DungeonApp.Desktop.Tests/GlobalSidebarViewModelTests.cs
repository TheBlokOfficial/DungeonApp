using System.Threading.Tasks;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The campaign position (docs/architecture.md, "Pasek boczny: trzy kategorie"): the one Kampania-
/// category row that belongs to the frame itself, not to the chosen system. Covers the bug report
/// that started this fix - "po wyborze systemu ... na pasku nie ma pozycji, która [półkę]
/// reprezentuje" - by asserting the row exists straight out of construction, and the three state
/// transitions docs/architecture.md's "Pozycja kampanii zmienia się razem ze stanem" names: selected
/// by default, relabelled on open, and back to the shelf (still selected) on close.
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
    public void Opening_a_campaign_relabels_the_campaign_position_to_the_campaigns_own_name()
    {
        var sidebar = BuildSidebar();

        sidebar.SetCampaignOpen(true, "Klątwa Strahda");

        Assert.Equal("Klątwa Strahda", sidebar.CampaignPositionItem.Label);
    }

    [Fact]
    public void Closing_the_campaign_restores_the_shelf_label_and_keeps_the_position_selected()
    {
        var sidebar = BuildSidebar();
        sidebar.SetCampaignOpen(true, "Klątwa Strahda");
        sidebar.ActivateCampaignPosition();

        sidebar.SetCampaignOpen(false, campaignName: null);
        sidebar.ActivateCampaignPosition();

        Assert.Equal("Kampanie", sidebar.CampaignPositionItem.Label);
        Assert.True(sidebar.CampaignPositionItem.IsActive);
    }

    [Fact]
    public async Task Activating_the_campaign_position_after_a_different_row_was_selected_makes_it_the_only_active_row()
    {
        var declaration = new SystemTabDeclaration("sys.tab", "Tab", "icon", _ => new FakeTabContent());
        var sidebar = BuildSidebar(systemTabs: [declaration]);
        await ((AsyncCommand)sidebar.SystemTabItems[0].SelectCommand).ExecuteAsync();
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
            () => Task.CompletedTask);

        await ((AsyncCommand)sidebar.CampaignPositionItem.SelectCommand).ExecuteAsync();

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
            () => Task.CompletedTask);
}
