using DungeonApp.Domain.Campaigns;

namespace DungeonApp.Application.Campaigns;

public sealed class EnableCampaignModuleUseCase(ICampaignRepository campaignRepository)
{
    public async Task<CampaignSession> ExecuteAsync(
        EnableCampaignModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaignId = new CampaignId(request.CampaignId);
        var campaign = await campaignRepository.FindByIdAsync(campaignId, cancellationToken)
            ?? throw new CampaignNotFoundException(campaignId);

        campaign.Execute(new EnableCampaignModule(Guid.NewGuid(), new ModuleId(request.ModuleId), request.Reason));
        await campaignRepository.SaveAsync(campaign, cancellationToken);

        return CampaignSessionMapper.Map(campaign);
    }
}
