using System.Text.Json;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

public sealed class ScheduledWorldEventDuePersistenceAdapter : ICampaignEventPersistenceAdapter
{
    public const string Type = "core.scheduler.world-event-due.v1";

    public string EventType => Type;

    public bool CanWrite(ICampaignEventPayload payload) => payload is ScheduledWorldEventDue;

    public JsonElement WritePayload(ICampaignEventPayload payload, JsonSerializerOptions serializerOptions)
    {
        var due = payload as ScheduledWorldEventDue
            ?? throw new ArgumentException("Scheduler adapter can only write due-event payloads.", nameof(payload));

        return JsonSerializer.SerializeToElement(SchedulerEventPersistence.ToState(due.ScheduledEvent), serializerOptions);
    }

    public ICampaignEventPayload ReadPayload(JsonElement payload, JsonSerializerOptions serializerOptions)
    {
        var state = payload.Deserialize<SchedulerEventPersistence.ScheduledWorldEventState>(serializerOptions)
            ?? throw new InvalidDataException("Due world-event payload is missing.");

        return new ScheduledWorldEventDue(SchedulerEventPersistence.FromState(state));
    }
}
