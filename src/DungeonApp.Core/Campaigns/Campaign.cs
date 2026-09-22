using System;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The root of everything the GM owns. Immutable, the way every piece of a campaign's state is
/// (docs/architecture.md, "Gdzie mieszka stan"): a change replaces this whole object with a new one
/// carrying a new <see cref="Snapshot"/> rather than mutating one in place, which is what makes it
/// safe to hand the same instance to a read-only view and to the code computing the next change at
/// once.
/// </summary>
public sealed class Campaign
{
    private Campaign(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignStateSnapshot snapshot)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        Snapshot = snapshot;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>This campaign's whole state, read-only - every model its system declares.</summary>
    public CampaignStateSnapshot Snapshot { get; }

    public static Campaign Create(
        CampaignName name,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(timeProvider);

        return new Campaign(CampaignId.New(), name, timeProvider.GetUtcNow(), CampaignStateSnapshot.Empty);
    }

    /// <summary>
    /// Rebuilds a campaign that already exists on disk. Separate from <see cref="Create"/> because
    /// restoring must not mint a new identity or a new creation date - a load is not a creation.
    /// </summary>
    public static Campaign Restore(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Campaign(id, name, createdAt, snapshot);
    }

    /// <summary>
    /// A campaign identical to this one except for its state. The one way a change ever moves a
    /// campaign forward: <c>CampaignSession</c> applies a <see cref="CampaignChange"/> to
    /// <see cref="Snapshot"/> and replaces its whole <see cref="Campaign"/> with what this returns.
    /// </summary>
    public Campaign WithSnapshot(CampaignStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new Campaign(Id, Name, CreatedAt, snapshot);
    }
}
