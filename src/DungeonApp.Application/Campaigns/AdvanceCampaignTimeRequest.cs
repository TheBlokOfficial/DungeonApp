namespace DungeonApp.Application.Campaigns;

public sealed record AdvanceCampaignTimeRequest(Guid CampaignId, TimeSpan Elapsed, string Reason);
