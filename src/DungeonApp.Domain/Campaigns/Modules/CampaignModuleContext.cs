using DungeonApp.Domain.Campaigns.Events;

namespace DungeonApp.Domain.Campaigns.Modules;

/// <summary>
/// Per-dispatch context created by the campaign host. Modules cannot construct it themselves.
/// </summary>
public sealed class CampaignModuleContext
{
    private readonly Queue<PendingCampaignEvent> _pendingEvents;
    private readonly ModuleId _sourceModule;
    private readonly Guid _correlationId;
    private readonly Guid? _causationEventId;

    internal CampaignModuleContext(
        Queue<PendingCampaignEvent> pendingEvents,
        ModuleId sourceModule,
        Guid correlationId,
        Guid? causationEventId,
        CampaignTime currentTime)
    {
        _pendingEvents = pendingEvents;
        _sourceModule = sourceModule;
        _correlationId = correlationId;
        _causationEventId = causationEventId;
        CurrentTime = currentTime;
    }

    public CampaignTime CurrentTime { get; }

    public void Publish(ICampaignEventPayload payload, CampaignTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(payload);

        _pendingEvents.Enqueue(new PendingCampaignEvent(
            Guid.NewGuid(),
            _sourceModule,
            _correlationId,
            _causationEventId,
            occurredAt,
            payload));
    }

    internal sealed record PendingCampaignEvent(
        Guid EventId,
        ModuleId SourceModule,
        Guid CorrelationId,
        Guid? CausationEventId,
        CampaignTime OccurredAt,
        ICampaignEventPayload Payload);
}
