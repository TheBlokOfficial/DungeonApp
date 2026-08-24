namespace DungeonApp.Domain.Campaigns.Modules.Clock;

public sealed record AdvanceWorldTime : CampaignCommand
{
    public AdvanceWorldTime(Guid commandId, TimeSpan elapsed, string reason)
        : base(commandId, ClockModule.Id, reason)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed time must be positive.");
        }

        Elapsed = elapsed;
    }

    public TimeSpan Elapsed { get; }
}
