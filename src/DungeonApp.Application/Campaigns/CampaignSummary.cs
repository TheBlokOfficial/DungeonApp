namespace DungeonApp.Application.Campaigns;

/// <summary>
/// Lightweight information needed by the campaign selection screen.
/// </summary>
public sealed record CampaignSummary(Guid Id, string Name, IReadOnlyList<string> EnabledModuleIds);
