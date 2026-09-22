using System;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Core.Events;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The narrow window a system's own desk tool gets onto the open campaign - built by the system's
/// own Campaign-category tab factory (the desk tab, today the only consumer) from a
/// <see cref="CampaignTabContext"/> plus the system's own <see cref="IContentTypeCatalog"/>.
/// <para>
/// Deliberately not <see cref="CampaignTabContext"/> itself: that context carries no resolver, because
/// resolving an instance needs a type catalog and a Campaign-category tab in general has no reason to
/// know one. A tool reading its own content by name does, so this type exists specifically to add
/// <see cref="Resolver"/> on top of what the tab context already carries - registry, instances, event
/// bus and the one door any write goes through.
/// </para>
/// <para>
/// The constructor stays public rather than internal, the same way <see cref="Shell.CampaignSession"/>'s
/// does, precisely so a system's own test project (a separate assembly, with no reason to reference
/// this one's internals) can build a context directly against a hand-built
/// <see cref="CampaignTabContext"/>, without running the whole shell to get one.
/// </para>
/// </summary>
public sealed class CampaignToolContext
{
    private readonly CampaignTabContext _context;

    public CampaignToolContext(CampaignTabContext context, IContentTypeCatalog types)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(types);

        _context = context;
        Instances = context.Instances;
        Registry = context.Registry;
        Resolver = new InstanceResolver(context.Registry, types);
        Events = context.Events;
    }

    public CampaignInstances Instances { get; }

    public ContentRegistry Registry { get; }

    public InstanceResolver Resolver { get; }

    public CampaignEvents Events { get; }

    /// <summary>
    /// The one door any write a tool performs goes through. Delegates to
    /// <see cref="CampaignTabContext.ExecuteAsync"/> so a tool never reaches the repository directly
    /// and never forgets to save what it changed.
    /// </summary>
    public Task<string?> ExecuteAsync(Action operation) => _context.ExecuteAsync(operation);
}
