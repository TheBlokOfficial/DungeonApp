using System;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// Creates a campaign and hands it to the store. A use case with injected collaborators rather than
/// a static helper, because the clock and the repository are exactly what a test needs to replace.
/// </summary>
public sealed class CreateCampaign(ICampaignRepository repository, TimeProvider timeProvider)
{
    /// <summary>
    /// Validation happens before anything is stored, so a rejected name leaves the repository
    /// untouched. Callers that want a message instead of an exception ask
    /// <see cref="CampaignName.Validate"/> first.
    /// </summary>
    public async Task<Campaign> ExecuteAsync(string? name, CancellationToken cancellationToken = default)
    {
        var campaign = Campaign.Create(CampaignName.Create(name), timeProvider);

        await repository.SaveAsync(campaign, cancellationToken);

        return campaign;
    }
}
