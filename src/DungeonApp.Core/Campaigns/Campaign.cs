using System;
using System.Collections.Generic;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The root of everything the GM owns. State lives inside a campaign; shared definitions live
/// outside it in the registries and are only referenced from here.
/// <para>
/// The campaign hosts data blocks but does not interpret them: it knows which ones this build
/// registers and what is currently stored in each, and nothing about what any of it means.
/// </para>
/// </summary>
public sealed class Campaign
{
    private Campaign(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignDataBlocks dataBlocks,
        CampaignEvents events)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        DataBlocks = dataBlocks;
        Events = events;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    public CampaignDataBlocks DataBlocks { get; }

    /// <summary>
    /// This campaign's announcement channel. Exposed because the shell hosting the campaign has no
    /// other way to hear what changes inside it - a data block being written is news the GM wants,
    /// not only news for other listeners.
    /// </summary>
    public CampaignEvents Events { get; }

    public static Campaign Create(
        CampaignName name,
        DataBlockRegistry registry,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var events = new CampaignEvents();

        return new Campaign(
            CampaignId.New(),
            name,
            timeProvider.GetUtcNow(),
            CampaignDataBlocks.Create(registry, events),
            events);
    }

    /// <summary>
    /// Rebuilds a campaign that already exists on disk. Separate from <see cref="Create"/> because
    /// restoring must not mint a new identity or a new creation date - a load is not a creation.
    /// </summary>
    public static Campaign Restore(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        DataBlockRegistry registry,
        IReadOnlyDictionary<DataBlockId, object> values,
        IReadOnlyDictionary<DataBlockId, DataBlockUnreadableReason>? unreadable = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(values);

        var events = new CampaignEvents();

        return new Campaign(
            id,
            name,
            createdAt,
            CampaignDataBlocks.Hydrate(registry, events, values, unreadable),
            events);
    }
}
