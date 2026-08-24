using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Domain.Campaigns.Modules.Scheduler;

public sealed record ScheduledWorldEventDue(ScheduledWorldEvent ScheduledEvent) : ICampaignEventPayload;
