using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Loads the campaign shelf before any visual component knows about it. Holds the result in its own field
/// and exposes it to subsequent steps - <see cref="WarmCampaignDataStep"/> needs the same
/// summaries, while desk and campaign-page warmup (<see cref="WarmFrameChromeStep"/>,
/// <see cref="WarmSystemTabsStep"/>) needs the first campaign in this list, if one exists.
/// </summary>
public sealed class LoadCampaignShelfStep(CampaignLibraryViewModel campaignLibrary) : IStartupStep
{
    public IReadOnlyList<CampaignSummary> Summaries { get; private set; } = [];

    public string Describe() => "Wczytywanie biblioteki kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken) =>
        Summaries = await campaignLibrary.LoadAsync();

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
