using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// Covers the new consumer wiring this pass adds: a system's own desk tools reach
/// <see cref="PanelCatalog"/>, and <see cref="CampaignToolProvider"/> only reads the registry once a
/// campaign is actually opened - never at composition-root time, when no pack has loaded yet.
/// </summary>
public sealed class CampaignToolProviderTests
{
    private static readonly ContentId SetId = ContentId.Create("fake-set");

    [Fact]
    public void PanelCatalog_built_from_no_system_is_empty()
    {
        var catalog = PanelCatalog.For([]);

        Assert.Empty(catalog.All);
    }

    [Fact]
    public void A_systems_tools_reach_the_panel_catalog_and_are_findable()
    {
        var session = BuildSession();
        var fakeDescriptor = BuildDescriptor("fake.tool");
        var system = new FakeGameSystem(SetId, [], tools: _ => [fakeDescriptor]);
        var provider = new CampaignToolProvider([system], () => EmptyRegistry(), new FakeContentTypeCatalog());

        var catalog = PanelCatalog.For(provider.ToolsFor(session));

        Assert.Contains(catalog.All, descriptor => descriptor.Id == "fake.tool");
        Assert.Single(catalog.All);
        Assert.NotNull(catalog.Find("fake.tool"));
    }

    [Fact]
    public void Tools_from_every_installed_system_are_stitched_together()
    {
        var session = BuildSession();
        var first = new FakeGameSystem(ContentId.Create("first"), [], tools: _ => [BuildDescriptor("first.tool")]);
        var second = new FakeGameSystem(ContentId.Create("second"), [], tools: _ => [BuildDescriptor("second.tool")]);
        var provider = new CampaignToolProvider([first, second], () => EmptyRegistry(), new FakeContentTypeCatalog());

        var tools = provider.ToolsFor(session);

        Assert.Equal(["first.tool", "second.tool"], tools.Select(descriptor => descriptor.Id));
    }

    /// <summary>
    /// The registry Func is exactly what lets the provider be built at composition-root time, before
    /// any pack is loaded (see <see cref="CampaignToolProvider"/>'s own remarks) - a Func that throws
    /// until a test "loads" it proves that constructing the provider, and even holding it, never
    /// forces that Func before a campaign is opened.
    /// </summary>
    [Fact]
    public void The_registry_is_read_lazily_by_ToolsFor_never_at_construction()
    {
        var loaded = false;
        ContentRegistry Registry() => loaded
            ? EmptyRegistry()
            : throw new InvalidOperationException("The registry must not be read before a campaign opens.");

        var system = new FakeGameSystem(SetId, [], tools: _ => []);

        // Building the provider must not touch the Func at all.
        var provider = new CampaignToolProvider([system], Registry, new FakeContentTypeCatalog());

        loaded = true;
        var session = BuildSession();

        // Only ToolsFor - called here for the first time - actually reads the registry.
        var tools = provider.ToolsFor(session);

        Assert.Empty(tools);
    }

    private static WorkspacePanelDescriptor BuildDescriptor(string id) =>
        new(
            id,
            id,
            "DungeonIconUsers",
            WorkspacePanelGroup.World,
            new PanelPlacement(0, 0, WorkspaceGridSettings.ToolPanelMinWidth, WorkspaceGridSettings.ToolPanelMinHeight),
            new PanelConstraints(
                WorkspaceGridSettings.ToolPanelMinWidth,
                WorkspaceGridSettings.ToolPanelMinHeight,
                WorkspaceGridSettings.ToolPanelMaxWidth,
                WorkspaceGridSettings.ToolPanelMaxHeight),
            () => new object());

    private static ContentRegistry EmptyRegistry() => new([], [], [], []);

    private static CampaignSession BuildSession()
    {
        var campaign = Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System);
        return new CampaignSession(campaign, new NullRepository());
    }

    private sealed class NullRepository : ICampaignRepository
    {
        public Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Campaign?>(null);

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }

    /// <summary>A catalog with no installed set - enough for a provider whose fakes never call it.</summary>
    private sealed class FakeContentTypeCatalog : IContentTypeCatalog
    {
        public bool HasSet(ContentId set) => false;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = default;
            return false;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = "no content type installed.";
            return false;
        }
    }
}
