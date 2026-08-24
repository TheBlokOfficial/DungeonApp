using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Application.Campaigns;

public sealed class ScheduleWorldEventUseCase(ICampaignRepository campaignRepository)
{
    public async Task<CampaignSession> ExecuteAsync(
        ScheduleWorldEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaignId = new CampaignId(request.CampaignId);
        var campaign = await campaignRepository.FindByIdAsync(campaignId, cancellationToken)
            ?? throw new CampaignNotFoundException(campaignId);

        campaign.Execute(new ScheduleWorldEvent(Guid.NewGuid(), request.Delay, request.Title, request.Reason));
        await campaignRepository.SaveAsync(campaign, cancellationToken);

        return CampaignSessionMapper.Map(campaign);
    }
}
