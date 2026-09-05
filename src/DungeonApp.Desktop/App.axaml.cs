using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Dice;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Clock;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Dice;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.History;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Party;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Scheduler;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    private WorkspaceLayoutStore? _layoutStore;
    private ModuleCatalog? _modules;
    private JsonCampaignJournalStore? _journal;
    private JsonCampaignRepository? _campaigns;
    private CampaignWorkspacePreparationCache? _preparations;
    private CampaignLibraryViewModel? _campaignLibrary;
    private IStartupStep[]? _startupSteps;
    private AppShellViewModel? _shell;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");
        var settingsStore = new AppSettingsStore(appDataDirectory);
        var loaded = settingsStore.Load();
        var settings = Program.UiScaleProfileOverride is { } overrideProfile
            ? loaded with { ScaleProfile = overrideProfile }
            : loaded;

        UiScaleProfiles.Apply(this, settings.ScaleProfile);

        // Kept as fields rather than locals, because the shell needs them once the window is built.
        // Plain constructor injection: no container, and deliberately no service locator.
        _layoutStore = new WorkspaceLayoutStore(appDataDirectory);

        // The one place the built-in modules are named. A campaign whose save mentions a module
        // missing from here is refused rather than opened incomplete.
        _modules = new ModuleCatalog()
            .Register(ClockModule.Id, () => new ClockModule())
            .Register(SchedulerModule.Id, () => new SchedulerModule())
            .Register(PartyModule.Id, () => new PartyModule())
            .Register(DiceModule.Id, () => new DiceModule());

        // The campaign library lives with the user's documents, not in application data: a campaign
        // is meant to be a visible, portable, backup-able document rather than hidden app state.
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Campaigns");

        // A separate store from the campaign's own: the chronicle is append-only, grows without
        // limit, and losing it must never cost the campaign.
        _journal = new JsonCampaignJournalStore(libraryPath);

        _campaigns = new JsonCampaignRepository(libraryPath, _modules, _journal, TimeProvider.System);

        // Cache dzielony przez krok rozgrzewki stołu i przez otwarcie prawdziwej kampanii później -
        // to ta sama instancja, żeby rozgrzewka nie liczyła się drugi raz przy pierwszym otwarciu.
        _preparations = new CampaignWorkspacePreparationCache(_campaigns, _journal, _layoutStore);

        // Biblioteka kampanii zgłasza się tutaj, w korzeniu kompozycji, mimo że wywołanie zwrotne
        // otwierające kampanię prowadzi do metody na powłoce, która jeszcze nie istnieje - domyka się
        // nad polem `_shell` i rozstrzyga dopiero przy pierwszym kliknięciu, długo po tym jak
        // OnFrameworkInitializationCompleted zdąży tę powłokę zbudować.
        _campaignLibrary = new CampaignLibraryViewModel(
            _campaigns,
            new CreateCampaign(_campaigns, _modules, TimeProvider.System),
            _modules,
            id => _shell!.OpenCampaignAsync(id));

        // Jawna tablica - kolejność w niej JEST kolejnością wykonania. Rozgrzewka wizualna jest
        // rozbita na osobne kroki per typ panelu, żeby żaden jeden krok nie trzymał dispatchera przez
        // czas rozgrzania całej deski naraz (patrz WarmCampaignWorkspaceVisualStep o tym, czemu sam
        // stół kampanii został wyjątkiem).
        var libraryStep = new LoadCampaignLibraryStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, libraryStep);

        _startupSteps =
        [
            libraryStep,
            dataStep,
            new WarmCampaignWorkspaceVisualStep(_preparations, dataStep, _layoutStore, _campaigns, _journal),
            new WarmWorkspacePlaceholderStep(),
            new WarmPanelVisualStep(
                "Przygotowywanie panelu drużyny…",
                () => new PanelWindow { Width = 480, Height = 260, Content = new PartyPanelView() }),
            new WarmPanelVisualStep(
                "Przygotowywanie panelu zegara…",
                () => new PanelWindow { Width = 360, Height = 260, Content = new ClockPanelView() }),
            new WarmPanelVisualStep(
                "Przygotowywanie panelu kroniki…",
                () => new PanelWindow { Width = 480, Height = 320, Content = new HistoryPanelView() }),
            new WarmPanelVisualStep(
                "Przygotowywanie panelu harmonogramu…",
                () => new PanelWindow { Width = 360, Height = 320, Content = new SchedulerPanelView() }),
            new WarmPanelVisualStep(
                "Przygotowywanie panelu kości…",
                () => new PanelWindow { Width = 480, Height = 128, Content = new DicePanelView() })
        ];
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _shell = new AppShellViewModel(
                _layoutStore!,
                _campaigns!,
                _journal!,
                _campaignLibrary!,
                _preparations!,
                _startupSteps!);

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
