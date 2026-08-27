using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// Stands in for the store wherever a test is about a use case rather than about the disk. It lives
/// in the test project on purpose: production code that only ever serves tests is production code
/// nobody maintains.
/// </summary>
internal sealed class InMemoryCampaignRepository : ICampaignRepository
{
    private readonly Dictionary<CampaignId, Campaign> _campaigns = [];

    public int SaveCount { get; private set; }

    public Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _campaigns[campaign.Id] = campaign;
        SaveCount++;

        return Task.CompletedTask;
    }

    public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_campaigns.GetValueOrDefault(id));

    public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CampaignSummary>>(
            _campaigns.Values
                .Select(campaign => new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt))
                .OrderBy(summary => summary.Name.Value, StringComparer.CurrentCultureIgnoreCase)
                .ToArray());
}
