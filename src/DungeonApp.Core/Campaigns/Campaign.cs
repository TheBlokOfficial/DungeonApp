using System;
using System.Collections.Generic;
using DungeonApp.Core.Events;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The root of everything the GM owns. State lives inside a campaign; shared definitions live
/// outside it in the registries and are only referenced from here.
/// <para>
/// The campaign hosts modules but does not interpret them: it knows which are switched on and in
/// what order, and nothing about what any of them means.
/// </para>
/// </summary>
public sealed class Campaign
{
    private Campaign(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignModules modules,
        CampaignEvents events)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        Modules = modules;
        Events = events;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    public CampaignModules Modules { get; }

    /// <summary>
    /// This campaign's announcement channel. Exposed because the shell hosting the campaign has no
    /// other way to hear what its modules announce - a scheduled event coming due is news the GM
    /// wants, not only news for other modules.
    /// </summary>
    public CampaignEvents Events { get; }

    public static Campaign Create(
        CampaignName name,
        IEnumerable<ICampaignModule> modules,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var events = new CampaignEvents();

        return new Campaign(
            CampaignId.New(),
            name,
            timeProvider.GetUtcNow(),
            CampaignModules.Activate(modules, events),
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
        IEnumerable<ICampaignModule> modules)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(modules);

        var events = new CampaignEvents();

        return new Campaign(
            id,
            name,
            createdAt,
            CampaignModules.Activate(modules, events),
            events);
    }
}
