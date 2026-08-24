using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules;
using DungeonApp.Domain.Campaigns.Modules.Clock;

namespace DungeonApp.Application.Campaigns;

public sealed class CreateCampaignUseCase(ICampaignRepository campaignRepository, CampaignModuleCatalog moduleCatalog)
{
    public async Task<CampaignSession> ExecuteAsync(
        CreateCampaignRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var modules = request.EnableClock
            ? new ICampaignModule[] { moduleCatalog.Create(ClockModule.Id) }
            : [];
        var campaign = new Campaign(CampaignId.New(), request.Name, modules, moduleCatalog.Factories);

        await campaignRepository.SaveAsync(campaign, cancellationToken);

        return CampaignSessionMapper.Map(campaign);
    }
}
