namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// Elapsed time in a campaign. A ruleset may later project it onto its own calendar.
/// </summary>
public readonly record struct CampaignTime : IComparable<CampaignTime>
{
    public static CampaignTime Start { get; } = new(TimeSpan.Zero);

    public CampaignTime(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Campaign time cannot be negative.");
        }

        Elapsed = elapsed;
    }

    public TimeSpan Elapsed { get; }

    public CampaignTime AdvanceBy(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed time must be positive.");
        }

        return new CampaignTime(Elapsed.Add(elapsed));
    }

    public int CompareTo(CampaignTime other) => Elapsed.CompareTo(other.Elapsed);
}
