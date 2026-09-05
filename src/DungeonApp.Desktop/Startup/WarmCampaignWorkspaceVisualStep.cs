using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa <see cref="CampaignWorkspaceView"/>: skompilowany XAML, motywy desek i pierwszy layout.
/// Gdy na półce jest kampania, wiąże ją z prawdziwymi danymi z <see cref="WarmCampaignDataStep"/> -
/// wtedy rozgrzewka jest tożsama z tym, co zobaczy pierwsze otwarcie. Bez kampanii rozgrzewa sam
/// szkielet widoku.
/// <para>
/// Panele na prawdziwym stole pochodzą z zapisanego układu użytkownika
/// (<c>WorkspaceSurface.ItemsSource</c>), więc nie są tu statycznie wyliczalne per typ - stąd
/// rozgrzewka bierze cały widok deski naraz, zamiast rozbijać go na osobne kroki per panel.
/// </para>
/// </summary>
public sealed class WarmCampaignWorkspaceVisualStep(
    CampaignWorkspacePreparationCache preparations,
    WarmCampaignDataStep dataStep,
    WorkspaceLayoutStore layoutStore,
    ICampaignRepository campaigns) : IStartupStep
{
    private CampaignWorkspaceViewModel? _warmupViewModel;

    public string Describe() => "Rozgrzewanie stołu kampanii…";

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        if (dataStep.WarmupCampaignId is not { } id ||
            await preparations.PeekAsync(id, cancellationToken) is not { } preparation)
        {
            _warmupViewModel = null;
            return;
        }

        var session = new CampaignSession(preparation.Campaign, campaigns);
        _warmupViewModel = new CampaignWorkspaceViewModel(layoutStore, session, preparation);
    }

    public async Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
    {
        Control warmup = _warmupViewModel is null
            ? new CampaignWorkspaceView()
            : new CampaignWorkspaceView { DataContext = _warmupViewModel };

        try
        {
            await VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, warmup, cancellationToken);
        }
        finally
        {
            _warmupViewModel?.Dispose();
        }
    }
}
