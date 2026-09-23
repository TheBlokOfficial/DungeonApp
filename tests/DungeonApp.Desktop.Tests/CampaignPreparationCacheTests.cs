using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

public sealed class CampaignPreparationCacheTests
{
    private static readonly SystemId FakeSystemId = SystemId.Create("fake-system");

    [Fact]
    public async Task WarmAsync_MakesFirstTakeUsePreparedCampaign()
    {
        var campaign = Campaign.Create(CampaignName.Create("Rozgrzana"), TimeProvider.System, FakeSystemId);
        var repository = new CountingRepository(campaign);
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(repository, [system]);
        var summary = new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt, FakeSystemId);

        await cache.WarmAsync([summary]);
        var borrowed = await cache.PeekAsync(summary);
        var prepared = await cache.TakeAsync(summary);

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
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(repository, [system]);
        var summary = new CampaignSummary(id, CampaignName.Create("Usunięta"), DateTimeOffset.UtcNow, FakeSystemId);

        await cache.WarmAsync([summary]);

        await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(summary));
    }

    [Fact]
    public async Task TakeAsync_refuses_a_campaign_with_no_system_recorded()
    {
        var repository = new CountingRepository(null);
        var cache = new CampaignPreparationCache(repository, []);
        var summary = new CampaignSummary(CampaignId.New(), CampaignName.Create("Bez systemu"), DateTimeOffset.UtcNow, null);

        var exception = await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(summary));

        Assert.Equal(CampaignUnavailableReason.NoSystem, exception.Reason);
        Assert.Equal(0, repository.GetCount);
    }

    [Fact]
    public async Task TakeAsync_refuses_a_campaign_whose_system_is_not_compiled()
    {
        var repository = new CountingRepository(null);
        var cache = new CampaignPreparationCache(repository, []);
        var summary = new CampaignSummary(CampaignId.New(), CampaignName.Create("Obcy system"), DateTimeOffset.UtcNow, FakeSystemId);

        var exception = await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(summary));

        Assert.Equal(CampaignUnavailableReason.UnknownSystem, exception.Reason);
        Assert.Equal(0, repository.GetCount);
    }

    private sealed class CountingRepository(Campaign? campaign) : ICampaignRepository
    {
        public int GetCount { get; private set; }

        public Task SaveAsync(
            Campaign value, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Campaign?> GetAsync(
            CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
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
