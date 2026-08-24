namespace DungeonApp.Application.Campaigns;

public sealed class ListCampaignsUseCase(ICampaignRepository campaignRepository)
{
    public Task<IReadOnlyList<CampaignSummary>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        campaignRepository.ListAsync(cancellationToken);
}
