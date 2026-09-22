using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Events;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The narrow window a system's own tab in the Campaign sidebar category gets onto the open
/// campaign - built fresh by the shell for every campaign the GM opens, and handed to
/// <see cref="CampaignTabDeclaration.CreateContentAsync"/>.
/// <para>
/// Carries exactly what docs/tasks.md's step 1 brief names: the campaign's id, its instances, its
/// event bus, the one door any write goes through, and the content registry - and nothing else
/// reachable from here, in particular never <see cref="Shell.CampaignSession"/> or
/// <see cref="Core.Campaigns.Campaign"/> themselves. Deliberately no resolver either: a tab that
/// needs one - today, the desk's own tools - builds its own <see cref="CampaignToolContext"/> from
/// this context plus its own type catalog, the same way the desk always has.
/// </para>
/// </summary>
public sealed class CampaignTabContext
{
    private readonly CampaignSession _session;

    public CampaignTabContext(CampaignSession session, ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(registry);

        _session = session;
        Registry = registry;
    }

    public CampaignId CampaignId => _session.Campaign.Id;

    public CampaignInstances Instances => _session.Campaign.Instances;

    public CampaignEvents Events => _session.Campaign.Events;

    public ContentRegistry Registry { get; }

    /// <summary>The one door any write a tab performs goes through - see <see cref="CampaignSession.ExecuteAsync"/>.</summary>
    public Task<string?> ExecuteAsync(Action operation) => _session.ExecuteAsync(operation);
}
