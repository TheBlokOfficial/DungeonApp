using System;
using System.Collections.Generic;
using DungeonApp.Core.Content;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The root of everything the GM owns. State lives inside a campaign; shared definitions live
/// outside it in the registries and are only referenced from here.
/// </summary>
public sealed class Campaign
{
    private Campaign(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignInstances instances,
        CampaignEvents events)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        Instances = instances;
        Events = events;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>This campaign's world: every entry brought in as a living instance.</summary>
    public CampaignInstances Instances { get; }

    /// <summary>
    /// This campaign's announcement channel. Exposed because the shell hosting the campaign has no
    /// other way to hear what changes inside it.
    /// </summary>
    public CampaignEvents Events { get; }

    public static Campaign Create(
        CampaignName name,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var events = new CampaignEvents();

        return new Campaign(
            CampaignId.New(),
            name,
            timeProvider.GetUtcNow(),
            CampaignInstances.Create(events),
            events);
    }

    /// <summary>
    /// Rebuilds a campaign that already exists on disk. Separate from <see cref="Create"/> because
    /// restoring must not mint a new identity or a new creation date - a load is not a creation.
    /// <para>
    /// <paramref name="instances"/> is optional because a campaign with not a single instance in it
    /// is a normal state, not a sign that something failed to load.
    /// </para>
    /// </summary>
    public static Campaign Restore(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        IReadOnlyCollection<CampaignInstance>? instances = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var events = new CampaignEvents();

        return new Campaign(
            id,
            name,
            createdAt,
            CampaignInstances.Hydrate(events, instances ?? []),
            events);
    }
}
