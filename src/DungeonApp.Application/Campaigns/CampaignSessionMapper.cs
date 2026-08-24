using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules.Clock;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Application.Campaigns;

internal static class CampaignSessionMapper
{
    public static CampaignSession Map(Campaign campaign)
    {
        var clock = campaign.Modules.OfType<ClockModule>().SingleOrDefault();
        var scheduler = campaign.Modules.OfType<SchedulerModule>().SingleOrDefault();
        var history = campaign.History.Select(MapHistory).ToList().AsReadOnly();
        var scheduledEvents = scheduler?.ScheduledEvents
            .Select(scheduledEvent => new ScheduledWorldEventInfo(
                scheduledEvent.Id,
                scheduledEvent.DueAt.Elapsed,
                scheduledEvent.Title,
                scheduledEvent.Reason))
            .ToList()
            .AsReadOnly() ?? [];

        return new CampaignSession(
            campaign.Id.Value,
            campaign.Name,
            clock?.CurrentTime.Elapsed,
            campaign.EnabledModules.Select(module => module.Id.Value).ToList().AsReadOnly(),
            scheduledEvents,
            history);
    }

    private static CampaignHistoryEntry MapHistory(Domain.Campaigns.Events.CampaignEvent campaignEvent) =>
        campaignEvent.Payload switch
        {
            WorldTimeAdvanced timeAdvanced => new CampaignHistoryEntry(
                campaignEvent.SequenceNumber,
                "Czas świata przesunięty",
                $"+{timeAdvanced.Elapsed:g} — {timeAdvanced.Reason}",
                campaignEvent.OccurredAt.Elapsed),
            CampaignModuleEnabled moduleEnabled => new CampaignHistoryEntry(
                campaignEvent.SequenceNumber,
                "Włączono moduł",
                $"{moduleEnabled.ModuleId} — {moduleEnabled.Reason}",
                campaignEvent.OccurredAt.Elapsed),
            WorldEventScheduled scheduled => new CampaignHistoryEntry(
                campaignEvent.SequenceNumber,
                "Zaplanowano zdarzenie świata",
                $"{scheduled.ScheduledEvent.Title} — termin: {scheduled.ScheduledEvent.DueAt.Elapsed:g}",
                campaignEvent.OccurredAt.Elapsed),
            ScheduledWorldEventDue due => new CampaignHistoryEntry(
                campaignEvent.SequenceNumber,
                "Termin zdarzenia nadszedł",
                due.ScheduledEvent.Title,
                campaignEvent.OccurredAt.Elapsed),
            _ => new CampaignHistoryEntry(
                campaignEvent.SequenceNumber,
                campaignEvent.Payload.GetType().Name,
                "W kampanii wystąpiło zdarzenie.",
                campaignEvent.OccurredAt.Elapsed)
        };
}
