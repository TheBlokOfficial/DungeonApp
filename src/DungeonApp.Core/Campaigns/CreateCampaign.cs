using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// Creates a campaign and hands it to the store. A use case with injected collaborators rather than
/// a static helper, because the registry, the clock and the repository are exactly what a test needs
/// to replace.
/// </summary>
public sealed class CreateCampaign(
    ICampaignRepository repository,
    DataBlockRegistry registry,
    TimeProvider timeProvider)
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
        var campaign = Campaign.Create(campaignName, registry, timeProvider);

        await repository.SaveAsync(campaign, cancellationToken);

        return campaign;
    }
}
