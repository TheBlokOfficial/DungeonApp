using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

internal static class SchedulerEventPersistence
{
    public static ScheduledWorldEventState ToState(ScheduledWorldEvent scheduledEvent) => new(
        scheduledEvent.Id,
        scheduledEvent.DueAt.Elapsed.Ticks,
        scheduledEvent.Title,
        scheduledEvent.Reason);

    public static ScheduledWorldEvent FromState(ScheduledWorldEventState state) => new(
        state.Id,
        new CampaignTime(TimeSpan.FromTicks(state.DueAtTicks)),
        state.Title,
        state.Reason);

    internal sealed record ScheduledWorldEventState(Guid Id, long DueAtTicks, string Title, string Reason);
}
