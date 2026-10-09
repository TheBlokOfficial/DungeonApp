using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Systems;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.Tests.Workspace;

/// <summary>Builds the desk's view model over an empty in-memory campaign and a system with no content, for tests that look at windows and layout.</summary>
internal static class TestDesk
{
    public static CampaignWorkspaceViewModel Create(
        WorkspaceLayoutStore store,
        string workspaceId,
        IReadOnlyList<WorkspacePanelDescriptor> tools,
        WorkspaceLayout? layout = null)
    {
        var session = new CampaignSession(
            Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System),
            new InMemoryCampaignRepository(),
            WorldModels.Combine([]));
        var source = WorldCatalogSource.Empty;
        var entries = new CampaignEntriesContext(new CampaignTabContext(session), source.Registry, source.Types);

        return new CampaignWorkspaceViewModel(
            store,
            workspaceId,
            layout ?? WorkspaceLayout.Empty,
            tools,
            () => Task.CompletedTask,
            entries,
            new FakeGameSystem(SystemId.Create("fake")));
    }
}
