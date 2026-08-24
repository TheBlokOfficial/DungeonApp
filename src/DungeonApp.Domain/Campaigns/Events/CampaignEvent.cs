namespace DungeonApp.Domain.Campaigns.Events;

/// <summary>
/// An auditable event in one campaign's ordered history.
/// </summary>
public sealed record CampaignEvent(
    Guid EventId,
    long SequenceNumber,
    ModuleId SourceModule,
    Guid CorrelationId,
    Guid? CausationEventId,
    CampaignTime OccurredAt,
    ICampaignEventPayload Payload);
