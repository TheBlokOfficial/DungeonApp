using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Desktop.Tests;

public sealed class CampaignWorkspacePreparationCacheTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        $"DungeonApp-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task WarmAsync_MakesFirstTakeUsePreparedCampaign()
    {
        var campaign = Campaign.Create(CampaignName.Create("Rozgrzana"), new DataBlockRegistry(), TimeProvider.System);
        var repository = new CountingRepository(campaign);
        var cache = new CampaignWorkspacePreparationCache(
            repository,
            new WorkspaceLayoutStore(_directory));
        var summary = new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt);

        await cache.WarmAsync([summary]);
        var borrowed = await cache.PeekAsync(campaign.Id);
        var prepared = await cache.TakeAsync(campaign.Id);

        Assert.NotNull(borrowed);
        Assert.Same(borrowed, prepared);
        Assert.Same(campaign, prepared.Campaign);
        Assert.Equal(1, repository.GetCount);
        Assert.True(prepared.Layout.IsEmpty);
    }

    [Fact]
    public async Task WarmAsync_DoesNotBlockStartupForRemovedCampaign()
    {
        var id = CampaignId.New();
        var repository = new CountingRepository(null);
        var cache = new CampaignWorkspacePreparationCache(
            repository,
            new WorkspaceLayoutStore(_directory));
        var summary = new CampaignSummary(id, CampaignName.Create("Usunięta"), DateTimeOffset.UtcNow);

        await cache.WarmAsync([summary]);

        await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(id));
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_directory))
        {
            System.IO.Directory.Delete(_directory, recursive: true);
        }
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

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }
}
