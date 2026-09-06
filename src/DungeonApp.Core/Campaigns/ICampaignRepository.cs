using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The port the campaign use cases require. Asynchronous from the first line because the only
/// planned adapter writes files - widening a synchronous port later would touch every caller.
/// </summary>
public interface ICampaignRepository
{
    Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default);

    /// <summary>Returns null for an unknown id: a missing campaign is an answer, not a failure.</summary>
    Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default);

    Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The shelf, not the contents. Ordering is deterministic so the library screen never reshuffles
    /// between reads; which order the GM actually sees is the screen's decision.
    /// </summary>
    Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default);
}
