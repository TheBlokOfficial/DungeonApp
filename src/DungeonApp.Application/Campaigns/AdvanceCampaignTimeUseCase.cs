using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules.Clock;

namespace DungeonApp.Application.Campaigns;

/// <summary>
/// Orchestrates time advancement. Rules and validation remain inside the clock module.
/// </summary>
public sealed class AdvanceCampaignTimeUseCase(ICampaignRepository campaignRepository)
{
    public async Task<IReadOnlyList<CampaignEvent>> ExecuteAsync(
        AdvanceCampaignTimeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaignId = new Domain.Campaigns.CampaignId(request.CampaignId);
        var campaign = await campaignRepository.FindByIdAsync(campaignId, cancellationToken)
            ?? throw new CampaignNotFoundException(campaignId);

        var committedEvents = campaign.Execute(
            new AdvanceWorldTime(Guid.NewGuid(), request.Elapsed, request.Reason));

        await campaignRepository.SaveAsync(campaign, cancellationToken);

        return committedEvents;
    }
}
