using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    // Handed in through the constructor rather than discovered anywhere below - see
    // DungeonApp.App/Program.cs. Not a service locator: everything built from this list
    // (the two aggregates below) is still plain constructor injection into the pieces that need it.
    private readonly IReadOnlyList<IGameSystem> _systems;

    private WorkspaceLayoutStore? _layoutStore;
    private JsonCampaignRepository? _campaigns;
    private CampaignWorkspacePreparationCache? _preparations;
    private CampaignLibraryViewModel? _campaignLibrary;
    private LoadContentPacksStep? _contentPacksStep;
    private ContentPresentationAggregate? _presentation;
    private CampaignToolProvider? _toolProvider;
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

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");
        // Kept as fields rather than locals, because the shell needs them once the window is built.
        // Plain constructor injection: no container, and deliberately no service locator.
        _layoutStore = new WorkspaceLayoutStore(appDataDirectory);

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

        var contentTypes = new ContentTypeCatalogAggregate(_systems);
        _presentation = new ContentPresentationAggregate(_systems);

        _contentPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPath, contentTypes));

        // Built once, here, alongside the other aggregates over _systems - never per campaign
        // open. The registry Func mirrors the one handed to the shell below: packs are not loaded
        // yet at this point in Initialize, so reading _contentPacksStep.Registry has to wait for
        // ToolsFor, called only once a campaign actually opens.
        _toolProvider = new CampaignToolProvider(_systems, () => _contentPacksStep!.Registry, contentTypes);

        // Cache dzielony przez krok rozgrzewki stołu i przez otwarcie prawdziwej kampanii później -
        // to ta sama instancja, żeby rozgrzewka nie liczyła się drugi raz przy pierwszym otwarciu.
        _preparations = new CampaignWorkspacePreparationCache(_campaigns, _layoutStore);

        // Biblioteka kampanii zgłasza się tutaj, w korzeniu kompozycji, mimo że wywołanie zwrotne
        // otwierające kampanię prowadzi do metody na powłoce, która jeszcze nie istnieje - domyka się
        // nad polem `_shell` i rozstrzyga dopiero przy pierwszym kliknięciu, długo po tym jak
        // OnFrameworkInitializationCompleted zdąży tę powłokę zbudować.
        _campaignLibrary = new CampaignLibraryViewModel(
            _campaigns,
            new CreateCampaign(_campaigns, TimeProvider.System),
            id => _shell!.OpenCampaignAsync(id));

        // Jawna tablica - kolejność w niej JEST kolejnością wykonania.
        var libraryStep = new LoadCampaignLibraryStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, libraryStep);

        _startupSteps =
        [
            // Paczki treści przed półką kampanii (architecture.md, "Przepływy"): rejestr musi istnieć zanim
            // cokolwiek próbuje rozwiązywać wobec niego referencje.
            _contentPacksStep,
            libraryStep,
            dataStep,
            new WarmCampaignWorkspaceVisualStep(_preparations, dataStep, _layoutStore, _campaigns, _toolProvider),
            new WarmWorkspacePlaceholderStep()
        ];
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _shell = new AppShellViewModel(
                _layoutStore!,
                _campaigns!,
                _campaignLibrary!,
                _preparations!,
                _startupSteps!,
                () => _contentPacksStep!.Registry,
                _presentation!,
                _toolProvider!);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _shell
            };

            // The last reliable moment to write a pending desk arrangement. Exit does not run on a
            // hard kill, so this is where the debounced layout writer is flushed.
            desktop.ShutdownRequested += (_, _) => _shell?.FlushPendingState();
            desktop.MainWindow.Closing += (_, _) => _shell?.FlushPendingState();

            // AppShellView starts preparation from its Loaded event. That ordering guarantees a
            // lightweight first frame before data preloading and visual warmup begin.
        }

        base.OnFrameworkInitializationCompleted();
    }
}
