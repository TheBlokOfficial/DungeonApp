using System;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The root of everything the GM owns. State lives inside a campaign; shared definitions live
/// outside it in the registries and are only referenced from here.
/// <para>
/// This increment carries identity alone. Modules, world state and the journal arrive in later
/// increments, and each of them will hang off this type rather than replacing it.
/// </para>
/// </summary>
public sealed class Campaign
{
    private Campaign(CampaignId id, CampaignName name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    public static Campaign Create(CampaignName name, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(timeProvider);

        return new Campaign(CampaignId.New(), name, timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Rebuilds a campaign that already exists on disk. Separate from <see cref="Create"/> because
    /// restoring must not mint a new identity or a new creation date - a load is not a creation.
    /// </summary>
    public static Campaign Restore(CampaignId id, CampaignName name, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new Campaign(id, name, createdAt);
    }
}
