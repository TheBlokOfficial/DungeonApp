using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignWorkspace;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa dane każdej kampanii z półki (odczyt repozytorium, układu biurka i kroniki) - czysta
/// praca w tle, bez dotknięcia UI. Zapamiętuje kampanię do wizualnej rozgrzewki: pierwszą z półki,
/// jeśli jakaś istnieje.
/// </summary>
public sealed class WarmCampaignDataStep(
    CampaignWorkspacePreparationCache preparations,
    LoadCampaignLibraryStep libraryStep) : IStartupStep
{
    private CampaignId? _warmupCampaignId;

    public CampaignId? WarmupCampaignId => _warmupCampaignId;

    public string Describe() => "Przygotowywanie stołów kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        var summaries = libraryStep.Summaries;
        await preparations.WarmAsync(summaries, cancellationToken);

        _warmupCampaignId = summaries.Count > 0 ? summaries[0].Id : null;
    }

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
