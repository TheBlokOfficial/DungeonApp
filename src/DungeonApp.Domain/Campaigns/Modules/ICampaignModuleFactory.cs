namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Runtime registration point for an optional module. Factories are supplied by composition,
/// never created by UI code or persisted with a campaign.
/// </summary>
public interface ICampaignModuleFactory
{
    ModuleId ModuleId { get; }

    ICampaignModule Create();
}
