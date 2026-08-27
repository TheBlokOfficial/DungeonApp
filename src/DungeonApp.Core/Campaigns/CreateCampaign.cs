using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// Creates a campaign and hands it to the store. A use case with injected collaborators rather than
/// a static helper, because the catalogue, the clock and the repository are exactly what a test
/// needs to replace.
/// </summary>
public sealed class CreateCampaign(
    ICampaignRepository repository,
    ModuleCatalog catalog,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Everything that can be refused is refused before anything is stored, so a rejected name or an
    /// impossible module set leaves no half-created campaign behind. Callers that want a message
    /// instead of an exception ask <see cref="CampaignName.Validate"/> first.
    /// </summary>
    public async Task<Campaign> ExecuteAsync(
        string? name,
        IReadOnlyList<ModuleId>? modules = null,
        CancellationToken cancellationToken = default)
    {
        var campaignName = CampaignName.Create(name);
        var active = CampaignModules.Activate((modules ?? []).Select(catalog.Create));
        var campaign = Campaign.Create(campaignName, active, timeProvider);

        await repository.SaveAsync(campaign, cancellationToken);

        return campaign;
    }
}
