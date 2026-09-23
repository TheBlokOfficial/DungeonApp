using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The narrow window a system's own tab in the Campaign sidebar category gets onto the open
/// campaign - built fresh by the shell for every campaign the GM opens, and handed to
/// <see cref="CampaignTabDeclaration.CreateContentAsync"/>.
/// <para>
/// Carries the campaign's id, a read-only <see cref="Snapshot"/>, the one door any change goes
/// through, and a subscription to what changed - and nothing else reachable from here, in particular
/// never <see cref="Shell.CampaignSession"/> or <see cref="Core.Campaigns.Campaign"/> themselves, no
/// object with a method that mutates state (docs/architecture.md, "Gdzie mieszka stan"), and no
/// content registry: the frame does not know what an entry is, so it has nothing to carry here. A tab
/// that needs entries - today, the desk's own tools - builds its own <c>CampaignEntriesContext</c> (in
/// the entries library, which the frame does not reference and so cannot name by <c>cref</c>) from
/// this context plus its own registry and type catalog, the same way the desk always has.
/// </para>
/// </summary>
public sealed class CampaignTabContext
{
    private readonly CampaignSession _session;

    public CampaignTabContext(CampaignSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    public CampaignId CampaignId => _session.Campaign.Id;

    /// <summary>The campaign's whole state, read-only, as of the last change this context has heard about.</summary>
    public CampaignStateSnapshot Snapshot => _session.Campaign.Snapshot;

    /// <summary>The one door any change a tab makes goes through - see <see cref="CampaignSession.ChangeAsync"/>.</summary>
    public Task<CampaignChangeResult> ChangeAsync(CampaignChange change) => _session.ChangeAsync(change);

    /// <summary>Raised after a change commits (or fails to save) - see <see cref="CampaignSession.Changed"/>.</summary>
    public event Action<CampaignStateSnapshot>? Changed
    {
        add => _session.Changed += value;
        remove => _session.Changed -= value;
    }
}
