using DungeonApp.Domain.Campaigns;

namespace DungeonApp.Application.Campaigns;

public sealed class GetCampaignSessionUseCase(ICampaignRepository campaignRepository)
{
    public async Task<CampaignSession> ExecuteAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var id = new CampaignId(campaignId);
        var campaign = await campaignRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new CampaignNotFoundException(id);

        return CampaignSessionMapper.Map(campaign);
    }
}
