namespace DungeonApp.Domain.Campaigns.Modules.Scheduler;

public sealed record ScheduleWorldEvent : CampaignCommand
{
    public ScheduleWorldEvent(Guid commandId, TimeSpan delay, string title, string reason)
        : base(commandId, SchedulerModule.Id, reason)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Scheduled delay must be positive.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Scheduled event title cannot be empty.", nameof(title));
        }

        Delay = delay;
        Title = title.Trim();
    }

    public TimeSpan Delay { get; }

    public string Title { get; }
}
