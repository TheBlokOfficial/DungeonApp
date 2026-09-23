using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
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
    // DungeonApp.App/Program.cs. Not a service locator: everything built from this list is still
    // plain constructor injection into the pieces that need it.
    private readonly IReadOnlyList<IGameSystem> _systems;

    private JsonCampaignRepository? _campaigns;
    private CampaignPreparationCache? _preparations;
    private CampaignLibraryViewModel? _campaignLibrary;
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

        // Usunięcie kampanii w działającej aplikacji trafia do Kosza systemu, nie znika trwale - tak,
        // żeby przypadkowe kliknięcie dało się cofnąć. JsonCampaignRepository nie zna Kosza - to
        // wybór korzenia kompozycji, wstrzyknięty, żeby testy (które budują ten magazyn same, z
        // wariantem trwałym) nigdy nie mogły trafić do prawdziwego Kosza użytkownika.
        _campaigns = new JsonCampaignRepository(libraryPath, DeleteDirectoryToRecycleBin);

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
        // najpierw każdy wkompilowany system przygotowuje własną treść (paczki, karty - jego własne
        // kroki startowe, rama nie wie, co robią), potem półka, dane każdej kampanii z półki, chrom
        // ramy (ekran wyboru, półka, pasek boczny w obu stanach, strona kampanii) i na końcu zakładki
        // każdego wkompilowanego systemu (jego rejestr, jego biurko z narzędziami).
        var shelfStep = new LoadCampaignShelfStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, shelfStep);

        _startupSteps =
        [
            .. _systems.SelectMany(system => system.StartupSteps),
            shelfStep,
            dataStep,
            new WarmFrameChromeStep(_systems, _campaignLibrary, _preparations, dataStep),
            new WarmSystemTabsStep(_systems, _campaigns, _preparations, dataStep)
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
                _startupSteps!);

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

    /// <summary>
    /// The Recycle Bin is a Windows shell concept; <see cref="OperatingSystem.IsWindows"/> is the
    /// source-level guard CA1416 asks for, not a suppression of it. Off Windows there is no bin to
    /// send anything to, so this falls back to the same permanent delete the store used before this
    /// injection existed.
    /// </summary>
    private static void DeleteDirectoryToRecycleBin(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                path,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }
        else
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
