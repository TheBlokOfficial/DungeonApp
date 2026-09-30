using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;

namespace DungeonApp.Testing;

/// <summary>
/// Stands in for the campaign store wherever a test is about behaviour rather than the disk.
/// <see cref="SaveCount"/> lets a test prove that a write actually went through the save door.
/// </summary>
public sealed class InMemoryCampaignRepository : ICampaignRepository
{
    // One dictionary models one directory; no test reads CampaignSummary.DirectorySystemId back,
    // so this only has to be a valid id.
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
        CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
        Task.FromResult(_campaigns.GetValueOrDefault(id));

    public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        _campaigns.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CampaignSummary>>(
            _campaigns.Values
                .Select(campaign => new CampaignSummary(
                    campaign.Id, campaign.Name, campaign.CreatedAt,
                    campaign.SystemId ?? FallbackDirectorySystemId, campaign.SystemId))
                .OrderBy(summary => summary.Name.Value, StringComparer.CurrentCultureIgnoreCase)
                .ToArray());
}
