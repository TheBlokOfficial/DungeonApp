using DungeonApp.Domain.Campaigns;

namespace DungeonApp.Application.Campaigns;

public sealed class CampaignNotFoundException(CampaignId campaignId)
    : InvalidOperationException($"Campaign '{campaignId.Value}' was not found.");
