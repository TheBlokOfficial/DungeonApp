using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Wczytuje półkę kampanii, zanim cokolwiek wizualne o niej wie. Wynik trzyma we własnym polu i
/// udostępnia następnym krokom - <see cref="WarmCampaignDataStep"/> potrzebuje tych samych
/// podsumowań, a rozgrzewka biurka i strony kampanii (<see cref="WarmFrameChromeStep"/>,
/// <see cref="WarmSystemContentStep"/>) - pierwszej kampanii z tej listy, jeśli jakaś istnieje.
/// </summary>
public sealed class LoadCampaignShelfStep(CampaignLibraryViewModel campaignLibrary) : IStartupStep
{
    public IReadOnlyList<CampaignSummary> Summaries { get; private set; } = [];

    public string Describe() => "Wczytywanie biblioteki kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken) =>
        Summaries = await campaignLibrary.LoadAsync();

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
