using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// The port the campaign use cases require. Asynchronous from the first line because the only
/// planned adapter writes files - widening a synchronous port later would touch every caller.
/// <para>
/// <paramref name="declarations"/> on <see cref="SaveAsync"/> and <see cref="GetAsync"/> is the
/// system's own list of state models (docs/architecture.md, "Gdzie mieszka stan": "Rama zapisuje,
/// system deklaruje") - never referenced by name here, so this port stays exactly what it was
/// before any model but instances existed.
/// </para>
/// </summary>
public interface ICampaignRepository
{
    Task SaveAsync(
        Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default);

    /// <summary>Returns null for an unknown id: a missing campaign is an answer, not a failure.</summary>
    Task<Campaign?> GetAsync(
        CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default);

    Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The shelf, not the contents. Ordering is deterministic so the library screen never reshuffles
    /// between reads; which order the GM actually sees is the screen's decision.
    /// </summary>
    Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default);
}
