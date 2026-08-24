namespace DungeonApp.Domain.Campaigns.Events;

/// <summary>
/// Auditable fact that a module became part of a campaign configuration.
/// </summary>
public sealed record CampaignModuleEnabled(ModuleId ModuleId, int StateVersion, string Reason) : ICampaignEventPayload;
