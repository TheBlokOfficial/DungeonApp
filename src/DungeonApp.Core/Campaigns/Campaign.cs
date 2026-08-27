using System;
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
    private Campaign(CampaignId id, CampaignName name, DateTimeOffset createdAt, CampaignModules modules)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        Modules = modules;
    }

    public CampaignId Id { get; }

    public CampaignName Name { get; }

    /// <summary>
    /// Read from an injected <see cref="TimeProvider"/> rather than <c>DateTimeOffset.UtcNow</c>, so
    /// a test can assert the recorded moment without owning the machine clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    public CampaignModules Modules { get; }

    public static Campaign Create(CampaignName name, CampaignModules modules, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(timeProvider);

        return new Campaign(CampaignId.New(), name, timeProvider.GetUtcNow(), modules);
    }

    /// <summary>
    /// Rebuilds a campaign that already exists on disk. Separate from <see cref="Create"/> because
    /// restoring must not mint a new identity or a new creation date - a load is not a creation.
    /// </summary>
    public static Campaign Restore(
        CampaignId id,
        CampaignName name,
        DateTimeOffset createdAt,
        CampaignModules modules)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(modules);

        return new Campaign(id, name, createdAt, modules);
    }
}
