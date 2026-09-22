using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    // Handed in through the constructor rather than discovered anywhere below - see
    // DungeonApp.App/Program.cs. Not a service locator: everything built from this list
    // (the two aggregates below) is still plain constructor injection into the pieces that need it.
    private readonly IReadOnlyList<IGameSystem> _systems;

    private JsonCampaignRepository? _campaigns;
    private CampaignPreparationCache? _preparations;
    private CampaignLibraryViewModel? _campaignLibrary;
    private LoadContentPacksStep? _contentPacksStep;
    private ContentTypeCatalogAggregate? _contentTypes;
    private IStartupStep[]? _startupSteps;
    private AppShellViewModel? _shell;

    public App(IReadOnlyList<IGameSystem> systems)
    {
        _systems = systems;
    }

    /// <summary>
    /// Exists only so Avalonia's own tooling (the XAML previewer, hot reload) can instantiate this
    /// class - it never runs in the shipped app, which always goes through the constructor above,
    /// wired by DungeonApp.App/Program.cs's <c>AppBuilder.Configure(Func&lt;App&gt;)</c> call. An
    /// empty system list only ever reaches <see cref="Initialize"/> under design-time tooling;
    /// outside of it, the guard at the top of that method turns this into a loud failure instead of
    /// a silently empty registry.
    /// </summary>
    public App() : this([])
    {
    }

    public override void Initialize()
    {
        if (_systems.Count == 0 && !Avalonia.Controls.Design.IsDesignMode)
        {
            throw new InvalidOperationException(
                "Aplikacja została zbudowana bez żadnego systemu. W praktyce oznacza to " +
                "pusty katalog typów, więc każdy wpis w każdej paczce zostanie nierozwiązany, a " +
                "rejestr treści pozostanie pusty. Napraw to w korzeniu kompozycji - " +
                "DungeonApp.App/Program.cs - przekazując tam co najmniej jeden system.");
        }

        AvaloniaXamlLoader.Load(this);

        // The campaign library lives with the user's documents, not in application data: a campaign
        // is meant to be a visible, portable, backup-able document rather than hidden app state.
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Campaigns");

        _campaigns = new JsonCampaignRepository(libraryPath);

        // Paczki treści są dokumentem użytkownika tak samo jak kampanie (architecture.md, "Gdzie
        // mieszka stan") - obok, nie pod
        // danymi aplikacji.
        var packsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Packs");

        _contentTypes = new ContentTypeCatalogAggregate(_systems);

        _contentPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPath, _contentTypes));

        // Cache dzielony przez rozgrzewkę pierwszej kampanii po wyborze systemu i przez otwarcie
        // prawdziwej kampanii później - to ta sama instancja, żeby rozgrzewka nie liczyła się drugi
        // raz przy pierwszym otwarciu. Nie zna magazynu układów biurka - ten dziś wystawia wyłącznie
        // system, w swoim własnym konstruktorze (DungeonApp.App/Program.cs), bo rama nie stawia
        // biurka. Deklaracje, którymi czyta jedną konkretną kampanię, nie są tu już z góry ustalone -
        // każda kampania niesie własny system w manifeście (docs/architecture.md, "Kampania należy do
        // jednego systemu"), więc cache sam dopasowuje go do jednego z `_systems` przy każdym odczycie.
        _preparations = new CampaignPreparationCache(_campaigns, _systems);

        // Biblioteka kampanii zgłasza się tutaj, w korzeniu kompozycji, mimo że wywołanie zwrotne
        // otwierające kampanię prowadzi do metody na powłoce, która jeszcze nie istnieje - domyka się
        // nad polem `_shell` i rozstrzyga dopiero przy pierwszym kliknięciu, długo po tym jak
        // OnFrameworkInitializationCompleted zdąży tę powłokę zbudować.
        _campaignLibrary = new CampaignLibraryViewModel(
            _campaigns,
            new CreateCampaign(_campaigns, TimeProvider.System),
            _preparations,
            _systems,
            summary => _shell!.OpenCampaignAsync(summary));

        // Jawna tablica - kolejność w niej JEST kolejnością wykonania. Wszystko wizualne, co GM mógłby
        // zobaczyć po raz pierwszy tuż po wyborze systemu, rozgrzewa się tutaj, przed pokazaniem ekranu
        // wyboru jako interaktywnego (docs/tasks.md, zadanie 1 - zamrożenie przy wyborze systemu
        // znikło stąd, nie skróceniem rozgrzewki, tylko przeniesieniem jej przed kurtynę startową):
        // paczki treści, półka, dane każdej kampanii z półki, potem chrom ramy (ekran wyboru, półka,
        // pasek boczny w obu stanach, strona kampanii) i na końcu zawartość każdego wkompilowanego
        // systemu (jego zakładki, karty wpisów, biurko z narzędziami).
        var shelfStep = new LoadCampaignShelfStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, shelfStep);

        _startupSteps =
        [
            _contentPacksStep,
            shelfStep,
            dataStep,
            new WarmFrameChromeStep(_systems, _campaignLibrary, _preparations, dataStep),
            new WarmSystemContentStep(_systems, () => _contentPacksStep!.Registry, _campaigns, _preparations, dataStep)
        ];
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _shell = new AppShellViewModel(
                _systems,
                _campaigns!,
                _campaignLibrary!,
                _preparations!,
                _startupSteps!,
                () => _contentPacksStep!.Registry);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _shell
            };

            // The last reliable moment to release whatever the active system's tabs are still
            // holding. Exit does not run on a hard kill, so this is where a desk tab's pending
            // layout write is flushed.
            desktop.ShutdownRequested += (_, _) => _shell?.FlushPendingState();
            desktop.MainWindow.Closing += (_, _) => _shell?.FlushPendingState();

            // AppShellView starts preparation from its Loaded event. That ordering guarantees a
            // lightweight first frame before data preloading and visual warmup begin.
        }

        base.OnFrameworkInitializationCompleted();
    }
}
