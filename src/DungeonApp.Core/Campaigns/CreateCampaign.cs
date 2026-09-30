using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// Creates a campaign and hands it to the store. A use case with injected collaborators rather than
/// a static helper, because the clock and the repository are exactly what a test needs to replace.
/// <para>
/// <paramref name="systemId"/> and <paramref name="declarations"/> on <see cref="ExecuteAsync"/> are
/// taken per call, not constructor-injected: creating a campaign always happens with one particular
/// system already active (the shelf that offers "Utwórz" only exists once a system is chosen), so the
/// caller - the composition root, wiring the shelf for whichever system the GM just picked - always
/// has both at hand by the time it calls this.
/// </para>
/// <para>
/// <paramref name="repositoriesBySystem"/> is one repository per compiled system - each scoped to
/// that system's own campaign directory, because a campaign belongs to the system whose directory it
/// lies in - fixed for the life of this instance, built once by the composition root from
/// <c>IGameSystem.Id</c>, never from a system named by literal.
/// <paramref name="systemId"/> on <see cref="ExecuteAsync"/> is what picks which one a given call
/// writes to.
/// </para>
/// </summary>
public sealed class CreateCampaign(
    IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Everything that can be refused is refused before anything is stored, so a rejected name leaves
    /// no half-created campaign behind. Callers that want a message instead of an exception ask
    /// <see cref="CampaignName.Validate"/> first.
    /// </summary>
    public async Task<Campaign> ExecuteAsync(
        string? name,
        SystemId systemId,
        IReadOnlyList<StateModelDeclaration> declarations,
        CancellationToken cancellationToken = default)
    {
        var campaignName = CampaignName.Create(name);
        var campaign = Campaign.Create(campaignName, timeProvider, systemId);

        await repositoriesBySystem[systemId].SaveAsync(campaign, declarations, cancellationToken);

        return campaign;
    }
}
