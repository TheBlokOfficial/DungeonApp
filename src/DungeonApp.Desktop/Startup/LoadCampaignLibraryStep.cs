using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Wczytuje półkę kampanii. Wynik trzyma we własnym polu i udostępnia następnym krokom - w tej
/// sekwencji <see cref="WarmCampaignDataStep"/> potrzebuje tych samych podsumowań.
/// </summary>
public sealed class LoadCampaignLibraryStep(CampaignLibraryViewModel campaignLibrary) : IStartupStep
{
    private IReadOnlyList<CampaignSummary> _summaries = [];

    public IReadOnlyList<CampaignSummary> Summaries => _summaries;

    public string Describe() => "Przygotowywanie biblioteki kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken) =>
        _summaries = await campaignLibrary.LoadAsync();

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
