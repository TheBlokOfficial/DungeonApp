namespace DungeonApp.Domain.Campaigns.Modules.Scheduler;

/// <summary>
/// A deliberate future fact waiting for campaign time to reach its due moment.
/// </summary>
public sealed record ScheduledWorldEvent
{
    public ScheduledWorldEvent(Guid id, CampaignTime dueAt, string title, string reason)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Scheduled event id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Scheduled event title cannot be empty.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Scheduled event reason cannot be empty.", nameof(reason));
        }

        Id = id;
        DueAt = dueAt;
        Title = title.Trim();
        Reason = reason.Trim();
    }

    public Guid Id { get; }

    public CampaignTime DueAt { get; }

    public string Title { get; }

    public string Reason { get; }
}
