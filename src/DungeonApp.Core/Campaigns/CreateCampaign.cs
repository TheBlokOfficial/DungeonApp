using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// Creates a campaign and hands it to the store. A use case with injected collaborators rather than
/// a static helper, because the clock and the repository are exactly what a test needs to replace.
/// <para>
/// <paramref name="declarations"/> is constructor-injected, the same way the clock and the
/// repository are, rather than taken per call: a brand new campaign is created before the GM has
/// picked a specific one to reopen, so there is no single open campaign's system to ask yet - see
/// the composition root for which declarations this actually gets.
/// </para>
/// </summary>
public sealed class CreateCampaign(
    ICampaignRepository repository,
    TimeProvider timeProvider,
    IReadOnlyList<StateModelDeclaration> declarations)
{
    /// <summary>
    /// Everything that can be refused is refused before anything is stored, so a rejected name leaves
    /// no half-created campaign behind. Callers that want a message instead of an exception ask
    /// <see cref="CampaignName.Validate"/> first.
    /// </summary>
    public async Task<Campaign> ExecuteAsync(
        string? name,
        CancellationToken cancellationToken = default)
    {
        var campaignName = CampaignName.Create(name);
        var campaign = Campaign.Create(campaignName, timeProvider);

        await repository.SaveAsync(campaign, declarations, cancellationToken);

        return campaign;
    }
}
