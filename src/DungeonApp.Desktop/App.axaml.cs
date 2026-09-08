using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tools.Counter;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Features.CampaignWorkspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    private WorkspaceLayoutStore? _layoutStore;
    private DataBlockRegistry? _dataBlocks;
    private CounterTool? _counterTool;
    private JsonCampaignRepository? _campaigns;
    private CampaignWorkspacePreparationCache? _preparations;
    private CampaignLibraryViewModel? _campaignLibrary;
    private LoadContentPacksStep? _contentPacksStep;
    private IStartupStep[]? _startupSteps;
    private AppShellViewModel? _shell;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");
        // Kept as fields rather than locals, because the shell needs them once the window is built.
        // Plain constructor injection: no container, and deliberately no service locator.
        _layoutStore = new WorkspaceLayoutStore(appDataDirectory);

        // Jedyne miejsce, w którym jawnie zgłaszają się wbudowane bloki danych i narzędzia.
        _counterTool = new CounterTool();
        _dataBlocks = new DataBlockRegistry()
            .Register(
                CounterTool.DataBlockId,
                CounterTool.DataBlockVersion,
                CounterTool.DataBlockShape);

        foreach (var usedDataBlock in _counterTool.Uses)
        {
            if (!_dataBlocks.Knows(usedDataBlock))
            {
                throw new InvalidOperationException(
                    $"Narzędzie licznika używa niezarejestrowanego bloku danych '{usedDataBlock}'.");
            }
        }

        // The campaign library lives with the user's documents, not in application data: a campaign
        // is meant to be a visible, portable, backup-able document rather than hidden app state.
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Campaigns");

        _campaigns = new JsonCampaignRepository(libraryPath, _dataBlocks);

        // Paczki treści są dokumentem użytkownika tak samo jak kampanie (sekcja 12) - obok, nie pod
        // danymi aplikacji.
        var packsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Packs");

        _contentPacksStep = new LoadContentPacksStep(new ContentPackLoader(packsPath));

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
            // Paczki treści przed półką kampanii (sekcja 15a): rejestr musi istnieć zanim
            // cokolwiek próbuje rozwiązywać wobec niego referencje.
            _contentPacksStep,
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
                _startupSteps!,
                () => _contentPacksStep!.Registry);

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
