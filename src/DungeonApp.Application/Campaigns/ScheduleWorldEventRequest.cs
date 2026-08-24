namespace DungeonApp.Application.Campaigns;

public sealed record ScheduleWorldEventRequest(Guid CampaignId, TimeSpan Delay, string Title, string Reason);
