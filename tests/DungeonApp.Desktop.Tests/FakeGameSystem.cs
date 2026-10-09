using System.Collections.Generic;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// A minimal <see cref="IGameSystem"/> shared by every Desktop test that needs one: the tab-lifecycle
/// tests (<c>ActiveSystemSessionTests</c>) use its <see cref="SystemTabs"/> and <see cref="CampaignTabs"/>,
/// others only need its <see cref="Id"/> to match a <c>CampaignSummary</c>. It carries no content-type
/// knowledge at all - the frame's own <see cref="IGameSystem"/> does not ask for any.
/// <para>
/// <paramref name="systemTabs"/> and <paramref name="campaignTabs"/> default to empty, so a test that
/// only needs an identity is not forced to think about tabs at all.
/// </para>
/// </summary>
internal sealed class FakeGameSystem(
    SystemId id,
    IReadOnlyList<SystemTabDeclaration>? systemTabs = null,
    IReadOnlyList<CampaignTabDeclaration>? campaignTabs = null) : IGameSystem
{
    public SystemId Id { get; } = id;

    public string DisplayName { get; } = id.ToString();

    public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = systemTabs ?? [];

    public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = campaignTabs ?? [];

    public IReadOnlyList<WorkspacePanelDescriptor> CreateDeskTools(CampaignTabContext context) => [];

    public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

    public IReadOnlyList<DungeonApp.Desktop.Systems.WorldEntityType> EntityTypes { get; } = [];

    public DungeonApp.Desktop.Systems.WorldCatalogSource GetWorldCatalogSource() =>
        DungeonApp.Desktop.Systems.WorldCatalogSource.Empty;

    public string? RowHint(DungeonApp.Core.Entries.ResolvedEntity entity) => null;

    public IReadOnlyList<IStartupStep> StartupSteps { get; } = [];
}
