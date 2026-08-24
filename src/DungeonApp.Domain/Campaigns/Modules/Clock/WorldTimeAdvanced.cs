using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Domain.Campaigns.Modules.Clock;

public sealed record WorldTimeAdvanced(
    CampaignTime From,
    CampaignTime To,
    TimeSpan Elapsed,
    string Reason) : ICampaignEventPayload;
