using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// Stands in for the campaign store wherever a Desktop test is about shell wiring rather than about
/// the disk. Duplicated from <c>DungeonApp.Content.Dnd5e.Tests.InMemoryCampaignRepository</c> rather
/// than referenced - each test project keeps its own copy of small fakes like this one, the same
/// discipline that project's own doc comment explains.
/// </summary>
internal sealed class InMemoryCampaignRepository : ICampaignRepository
{
    private readonly Dictionary<CampaignId, Campaign> _campaigns = [];

    public Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _campaigns[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_campaigns.GetValueOrDefault(id));

    public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        _campaigns.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampaignSummary>>(
            _campaigns.Values
                .Select(campaign => new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt))
                .ToArray());
}
