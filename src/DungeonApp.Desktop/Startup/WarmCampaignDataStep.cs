using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa dane każdej kampanii z półki (odczyt repozytorium) - czysta praca w tle, bez dotknięcia
/// UI. Zapamiętuje pierwszą kampanię z półki dla wizualnej rozgrzewki strony kampanii, biurka i
/// zakładek kampanii każdego systemu (<see cref="WarmFrameChromeStep"/>, <see cref="WarmSystemContentStep"/>) -
/// tak samo jak 555802f rozgrzewał jedną, pierwszą kampanię, nigdy całą półkę wizualnie.
/// </summary>
public sealed class WarmCampaignDataStep(
    CampaignPreparationCache preparations,
    LoadCampaignShelfStep shelfStep) : IStartupStep
{
    public CampaignId? WarmupCampaignId { get; private set; }

    public string Describe() => "Przygotowywanie kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        var summaries = shelfStep.Summaries;
        await preparations.WarmAsync(summaries, cancellationToken);

        WarmupCampaignId = summaries.Count > 0 ? summaries[0].Id : null;
    }

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
