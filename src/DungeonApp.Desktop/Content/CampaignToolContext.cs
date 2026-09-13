using System;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Core.Events;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The narrow window a content set's own desk tool gets onto the open campaign - built once per
/// <see cref="CampaignToolProvider.ToolsFor"/> call and handed to every <see cref="IContentSet.CreateTools"/>.
/// <para>
/// Deliberately not <see cref="CampaignSession"/> and not <see cref="Core.Campaigns.Campaign"/>
/// themselves: docs/architecture.md's "Warstwy i granice" lets a content set know what its own content
/// looks like, not everything a campaign happens to carry. A tool gets exactly the four things
/// "narzędzie czytające pole po nazwie mieszka w zestawie" needs - the campaign's instances, the
/// registry to resolve entries against, the resolver that does the resolving, and the one door any
/// write goes through - and nothing else reachable from here.
/// </para>
/// <para>
/// In production, only <see cref="CampaignToolProvider"/> ever builds one - a content set receives an
/// instance as a parameter and never constructs its own. The constructor stays public rather than
/// internal, the same way <see cref="CampaignSession"/>'s does, precisely so a content set's own test
/// project (a separate assembly, with no reason to reference this one's internals) can build a
/// context directly against a hand-built <see cref="CampaignSession"/> and registry, without running
/// the whole shell to get one.
/// </para>
/// </summary>
public sealed class CampaignToolContext
{
    private readonly CampaignSession _session;

    public CampaignToolContext(CampaignSession session, ContentRegistry registry, IContentTypeCatalog types)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(types);

        _session = session;
        Instances = session.Campaign.Instances;
        Registry = registry;
        Resolver = new InstanceResolver(registry, types);
        Events = session.Campaign.Events;
    }

    public CampaignInstances Instances { get; }

    public ContentRegistry Registry { get; }

    public InstanceResolver Resolver { get; }

    public CampaignEvents Events { get; }

    /// <summary>
    /// The one door any write a tool performs goes through. Delegates to
    /// <see cref="CampaignSession.ExecuteAsync"/> so a tool never reaches the repository directly and
    /// never forgets to save what it changed.
    /// </summary>
    public Task<string?> ExecuteAsync(Action operation) => _session.ExecuteAsync(operation);
}
