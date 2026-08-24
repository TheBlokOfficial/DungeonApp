using System.Text.Json;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Infrastructure.Campaigns.Persistence;

public sealed class WorldEventScheduledPersistenceAdapter : ICampaignEventPersistenceAdapter
{
    public const string Type = "core.scheduler.world-event-scheduled.v1";

    public string EventType => Type;

    public bool CanWrite(ICampaignEventPayload payload) => payload is WorldEventScheduled;

    public JsonElement WritePayload(ICampaignEventPayload payload, JsonSerializerOptions serializerOptions)
    {
        var scheduled = payload as WorldEventScheduled
            ?? throw new ArgumentException("Scheduler adapter can only write scheduled-event payloads.", nameof(payload));

        return JsonSerializer.SerializeToElement(SchedulerEventPersistence.ToState(scheduled.ScheduledEvent), serializerOptions);
    }

    public ICampaignEventPayload ReadPayload(JsonElement payload, JsonSerializerOptions serializerOptions)
    {
        var state = payload.Deserialize<SchedulerEventPersistence.ScheduledWorldEventState>(serializerOptions)
            ?? throw new InvalidDataException("Scheduled world-event payload is missing.");

        return new WorldEventScheduled(SchedulerEventPersistence.FromState(state));
    }
}
