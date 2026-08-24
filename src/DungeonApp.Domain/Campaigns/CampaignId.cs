namespace DungeonApp.Domain.Campaigns;

/// <summary>
/// Stable identity of a campaign. It is deliberately independent of storage technology.
/// </summary>
public readonly record struct CampaignId
{
    public CampaignId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Campaign id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CampaignId New() => new(Guid.NewGuid());
}
