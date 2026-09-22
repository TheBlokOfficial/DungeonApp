using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

public sealed class CampaignPreparationCacheTests
{
    [Fact]
    public async Task WarmAsync_MakesFirstTakeUsePreparedCampaign()
    {
        var campaign = Campaign.Create(CampaignName.Create("Rozgrzana"), TimeProvider.System);
        var repository = new CountingRepository(campaign);
        var cache = new CampaignPreparationCache(repository);
        var summary = new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt);

        await cache.WarmAsync([summary]);
        var borrowed = await cache.PeekAsync(campaign.Id);
        var prepared = await cache.TakeAsync(campaign.Id);

        Assert.NotNull(borrowed);
        Assert.Same(borrowed, prepared);
        Assert.Same(campaign, prepared);
        Assert.Equal(1, repository.GetCount);
    }

    [Fact]
    public async Task WarmAsync_DoesNotBlockStartupForRemovedCampaign()
    {
        var id = CampaignId.New();
        var repository = new CountingRepository(null);
        var cache = new CampaignPreparationCache(repository);
        var summary = new CampaignSummary(id, CampaignName.Create("Usunięta"), DateTimeOffset.UtcNow);

        await cache.WarmAsync([summary]);

        await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(id));
    }

    private sealed class CountingRepository(Campaign? campaign) : ICampaignRepository
    {
        public int GetCount { get; private set; }

        public Task SaveAsync(Campaign value, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(campaign?.Id == id ? campaign : null);
        }

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }
}
