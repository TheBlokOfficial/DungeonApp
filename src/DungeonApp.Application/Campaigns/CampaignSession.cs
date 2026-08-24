namespace DungeonApp.Application.Campaigns;

/// <summary>
/// Read model for an active-session UI. It prevents presentation code from reaching into module internals.
/// </summary>
public sealed record CampaignSession(
    Guid Id,
    string Name,
    TimeSpan? WorldTime,
    IReadOnlyList<string> EnabledModuleIds,
    IReadOnlyList<ScheduledWorldEventInfo> ScheduledWorldEvents,
    IReadOnlyList<CampaignHistoryEntry> History);

public sealed record CampaignHistoryEntry(long SequenceNumber, string Title, string Description, TimeSpan OccurredAt);

public sealed record ScheduledWorldEventInfo(Guid Id, TimeSpan DueAt, string Title, string Reason);
