using DungeonApp.Domain.Campaigns;

namespace DungeonApp.Application.Campaigns;

/// <summary>
/// Application port for loading and saving the aggregate root.
/// </summary>
public interface ICampaignRepository
{
    Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default);

    Task<Campaign?> FindByIdAsync(CampaignId id, CancellationToken cancellationToken = default);

    Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default);
}
