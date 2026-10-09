using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Warms every shelf campaign's data (repository read) - background work without touching
/// UI. Remembers the first shelf campaign for visual warmup of the desk and
/// each system's campaign tabs (<see cref="WarmSystemTabsStep"/>) - visually only the first campaign, never the whole shelf.
/// </summary>
public sealed class WarmCampaignDataStep(
    CampaignPreparationCache preparations,
    LoadCampaignShelfStep shelfStep) : IStartupStep
{
    public CampaignSummary? WarmupCampaignSummary { get; private set; }

    public string Describe() => "Przygotowywanie kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        var summaries = shelfStep.Summaries;
        await preparations.WarmAsync(summaries, cancellationToken);

        WarmupCampaignSummary = summaries.Count > 0 ? summaries[0] : null;
    }

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
