using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Desktop.Features.CampaignWorkspace;

/// <summary>
/// Everything that may touch storage before a campaign desk is mounted. It contains no Avalonia
/// controls and can therefore be assembled away from the UI thread during application startup.
/// </summary>
public sealed record CampaignWorkspacePreparation(
    Campaign Campaign,
    WorkspaceLayout Layout);
