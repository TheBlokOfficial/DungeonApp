using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Stands in for the campaign store wherever a test is about the view model rather than about the
/// disk. Duplicated from <c>DungeonApp.Core.Tests.Fakes.InMemoryCampaignRepository</c> rather than
/// referenced - this project already keeps <c>RepositoryRoot</c> as its own copy for the same reason:
/// a project reference between test projects would be a needless edge for the sake of one small fake.
/// <see cref="SaveCount"/> is what <see cref="CampaignInstancesToolViewModelTests"/> uses to prove a
/// write went through the campaign session's save door - the one door this system's tool is
/// required to use.
/// </summary>
internal sealed class InMemoryCampaignRepository : ICampaignRepository
{
    // A single in-memory dictionary models one directory, unlike the real per-system repository - no
    // test that uses this fake reads CampaignSummary.DirectorySystemId back, so this stand-in value
    // only has to be a valid one.
    private static readonly SystemId FallbackDirectorySystemId = SystemId.Create(DungeonApp.Content.Dnd5e.Dnd5eSystem.IdValue);

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
                .ToArray());
}
