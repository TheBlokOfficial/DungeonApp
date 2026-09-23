using System;
using System.Threading.Tasks;
using DungeonApp.Library.Entries;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;

namespace DungeonApp.Library.Entries.Desktop.Content;

/// <summary>
/// The narrow window a system's own desk tool gets onto the open campaign - built by the system's
/// own Campaign-category tab factory (the desk tab, today the only consumer) from a
/// <see cref="CampaignTabContext"/> plus the system's own registry and <see cref="IContentTypeCatalog"/>.
/// <para>
/// Deliberately not <see cref="CampaignTabContext"/> itself: that context carries neither a registry
/// nor a resolver, because the frame does not know what an entry is at all, and a Campaign-category
/// tab in general has no reason to know one either. A tool reading its own content by name does, so
/// this type exists specifically to add <see cref="Registry"/> and <see cref="Resolver"/> on top of
/// what the tab context already carries - a read-only snapshot, the one door any change goes through,
/// and a subscription to what changed.
/// </para>
/// <para>
/// The constructor stays public rather than internal, the same way
/// <see cref="DungeonApp.Desktop.Shell.CampaignSession"/>'s does, precisely so a system's own test
/// project (a separate assembly, with no reason to reference this one's internals) can build a
/// context directly against a hand-built <see cref="CampaignTabContext"/>, without running the whole
/// shell to get one.
/// </para>
/// </summary>
public sealed class CampaignEntriesContext
{
    private readonly CampaignTabContext _context;

    public CampaignEntriesContext(CampaignTabContext context, ContentRegistry registry, IContentTypeCatalog types)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(types);

        _context = context;
        Registry = registry;
        Resolver = new InstanceResolver(registry, types);
    }

    /// <summary>The campaign's whole state, read-only, as of the last change this context has heard about.</summary>
    public CampaignStateSnapshot Snapshot => _context.Snapshot;

    public ContentRegistry Registry { get; }

    public InstanceResolver Resolver { get; }

    /// <summary>The one door any change a tool makes goes through - delegates to <see cref="CampaignTabContext.ChangeAsync"/> so a tool never reaches the repository directly and never forgets to save what it changed.</summary>
    public Task<CampaignChangeResult> ChangeAsync(CampaignChange change) => _context.ChangeAsync(change);

    /// <summary>Raised after a change commits (or fails to save) - see <see cref="DungeonApp.Desktop.Shell.CampaignSession.Changed"/>.</summary>
    public event Action<CampaignStateSnapshot>? Changed
    {
        add => _context.Changed += value;
        remove => _context.Changed -= value;
    }
}
