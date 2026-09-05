using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    private WorkspaceLayoutStore? _layoutStore;
    private DataBlockRegistry? _dataBlocks;
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

        // The one place the built-in data blocks are named. A campaign whose save mentions a data
        // block missing from here is opened anyway, with that block left unread - see
        // CampaignDataBlocks.UnreadableBlocks. Empty for now: no data block ships in this build yet.
        _dataBlocks = new DataBlockRegistry();

        // The campaign library lives with the user's documents, not in application data: a campaign
        // is meant to be a visible, portable, backup-able document rather than hidden app state.
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Campaigns");

        _campaigns = new JsonCampaignRepository(libraryPath, _dataBlocks);

        // Cache dzielony przez krok rozgrzewki stołu i przez otwarcie prawdziwej kampanii później -
        // to ta sama instancja, żeby rozgrzewka nie liczyła się drugi raz przy pierwszym otwarciu.
        _preparations = new CampaignWorkspacePreparationCache(_campaigns, _layoutStore);

        // Biblioteka kampanii zgłasza się tutaj, w korzeniu kompozycji, mimo że wywołanie zwrotne
        // otwierające kampanię prowadzi do metody na powłoce, która jeszcze nie istnieje - domyka się
        // nad polem `_shell` i rozstrzyga dopiero przy pierwszym kliknięciu, długo po tym jak
        // OnFrameworkInitializationCompleted zdąży tę powłokę zbudować.
        _campaignLibrary = new CampaignLibraryViewModel(
            _campaigns,
            new CreateCampaign(_campaigns, _dataBlocks, TimeProvider.System),
            id => _shell!.OpenCampaignAsync(id));

        // Jawna tablica - kolejność w niej JEST kolejnością wykonania.
        var libraryStep = new LoadCampaignLibraryStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, libraryStep);

        _startupSteps =
        [
            libraryStep,
            dataStep,
            new WarmCampaignWorkspaceVisualStep(_preparations, dataStep, _layoutStore, _campaigns),
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
