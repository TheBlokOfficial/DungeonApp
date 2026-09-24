using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// Stands in for the store wherever a test is about a use case rather than about the disk. It lives
/// in the test project on purpose: production code that only ever serves tests is production code
/// nobody maintains.
/// </summary>
internal sealed class InMemoryCampaignRepository : ICampaignRepository
{
    // A single in-memory dictionary models one directory, unlike the real per-system repository - no
    // test that uses this fake reads CampaignSummary.DirectorySystemId back, so this stand-in value
    // only has to be a valid one.
    private static readonly SystemId FallbackDirectorySystemId = SystemId.Create("test-system");

    private readonly Dictionary<CampaignId, Campaign> _campaigns = [];

    public int SaveCount { get; private set; }

    public Task SaveAsync(
        Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
    {
        _campaigns[campaign.Id] = campaign;
        SaveCount++;

        return Task.CompletedTask;
    }

    public Task<Campaign?> GetAsync(
        CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
        => Task.FromResult(_campaigns.GetValueOrDefault(id));

    public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        _campaigns.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CampaignSummary>>(
            _campaigns.Values
                .Select(campaign => new CampaignSummary(
                    campaign.Id, campaign.Name, campaign.CreatedAt,
                    campaign.SystemId ?? FallbackDirectorySystemId, campaign.SystemId))
                .OrderBy(summary => summary.Name.Value, StringComparer.CurrentCultureIgnoreCase)
                .ToArray());
}
