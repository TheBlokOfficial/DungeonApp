using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

public sealed class CampaignPreparationCacheTests
{
    private static readonly SystemId FakeSystemId = SystemId.Create("fake-system");
    private static readonly SystemId OtherSystemId = SystemId.Create("other-system");

    [Fact]
    public async Task WarmAsync_MakesFirstTakeUsePreparedCampaign()
    {
        var campaign = Campaign.Create(CampaignName.Create("Rozgrzana"), TimeProvider.System, FakeSystemId);
        var repository = new CountingRepository(campaign);
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(RepositoriesFor((FakeSystemId, repository)), [system]);
        var summary = new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt, FakeSystemId, FakeSystemId);

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
        var cache = new CampaignPreparationCache(RepositoriesFor((FakeSystemId, repository)), [system]);
        var summary = new CampaignSummary(id, CampaignName.Create("Usunięta"), DateTimeOffset.UtcNow, FakeSystemId, FakeSystemId);

        await cache.WarmAsync([summary]);

        await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(summary));
    }

    /// <summary>
    /// docs/architecture.md, "Kampania należy do jednego systemu": a manifest that names no system at
    /// all belongs to the directory it was found in - the repository for that very directory is the
    /// one this reads with, and the campaign opens normally.
    /// </summary>
    [Fact]
    public async Task TakeAsync_reads_a_campaign_with_no_manifest_system_from_its_directorys_own_repository()
    {
        var campaign = Campaign.Create(CampaignName.Create("Bez systemu"), TimeProvider.System, FakeSystemId);
        var repository = new CountingRepository(campaign);
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(RepositoriesFor((FakeSystemId, repository)), [system]);
        var summary = new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt, FakeSystemId, SystemId: null);

        var prepared = await cache.TakeAsync(summary);

        Assert.Same(campaign, prepared);
        Assert.Equal(1, repository.GetCount);
    }

    /// <summary>
    /// docs/architecture.md, "Kampania należy do jednego systemu": a manifest naming a system other
    /// than the directory it lives in is refused before any disk read - whether or not that manifest
    /// system happens to be a compiled one.
    /// </summary>
    [Fact]
    public async Task TakeAsync_refuses_a_campaign_whose_manifest_names_a_different_system_than_its_directory()
    {
        var repository = new CountingRepository(null);
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(RepositoriesFor((FakeSystemId, repository)), [system]);
        var summary = new CampaignSummary(
            CampaignId.New(), CampaignName.Create("Cudza kampania"), DateTimeOffset.UtcNow, FakeSystemId, OtherSystemId);

        var exception = await Assert.ThrowsAsync<CampaignUnavailableException>(() => cache.TakeAsync(summary));

        Assert.Equal(CampaignUnavailableReason.MismatchedSystem, exception.Reason);
        Assert.Equal(0, repository.GetCount);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_reports_a_mismatched_system_as_such()
    {
        var repository = new CountingRepository(null);
        var system = new FakeGameSystem(FakeSystemId, []);
        var cache = new CampaignPreparationCache(RepositoriesFor((FakeSystemId, repository)), [system]);
        var summary = new CampaignSummary(
            CampaignId.New(), CampaignName.Create("Cudza kampania"), DateTimeOffset.UtcNow, FakeSystemId, OtherSystemId);

        var availability = await cache.CheckAvailabilityAsync(summary);

        Assert.Equal(CampaignAvailability.MismatchedSystem, availability);
    }

    private static IReadOnlyDictionary<SystemId, ICampaignRepository> RepositoriesFor(
        params (SystemId Id, ICampaignRepository Repository)[] entries)
    {
        var result = new Dictionary<SystemId, ICampaignRepository>();

        foreach (var (id, repository) in entries)
        {
            result[id] = repository;
        }

        return result;
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
