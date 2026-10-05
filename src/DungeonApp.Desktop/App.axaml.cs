using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Systems;
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

    private IReadOnlyDictionary<SystemId, ICampaignRepository>? _repositoriesBySystem;
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
    /// silently empty content tabs.
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
        Themes.SystemMotion.Apply(this);

        // Campaigns live with user documents, not application data: a campaign should be a visible,
        // portable, copyable document rather than hidden program state. One repository per
        // compiled-in system, in its own directory: a campaign belongs to the system
        // whose directory contains it. SystemDirectories derives this path generically from each
        // IGameSystem.Id, never from a specific system name.
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Deleting a campaign in the running app sends it to the system Recycle Bin rather than deleting permanently,
        // so an accidental click can be undone. JsonCampaignRepository knows nothing about the Bin - this
        // is a composition-root choice, injected so tests (which build their own repository with
        // permanent deletion) can never touch the user's real Recycle Bin.
        _repositoriesBySystem = _systems.ToDictionary(
            system => system.Id,
            system => (ICampaignRepository)new JsonCampaignRepository(
                system.Id,
                SystemDirectories.Campaigns(documentsPath, system.Id),
                DeleteDirectoryToRecycleBin));

        // Cache shared by first-campaign warmup after system selection and later opening of
        // a real campaign - same instance so warmup is not repeated
        // on first opening. Knows nothing about desk layout storage - only the system exposes
        // that in its own constructor (DungeonApp.App/Program.cs), because the shell does not build
        // the desk. Declarations used to read a particular campaign are not fixed here -
        // each campaign carries its own system in the manifest, so the cache matches it to one
        // of `_systems` on every read.
        _preparations = new CampaignPreparationCache(_repositoriesBySystem, _systems);

        // Campaign library is registered here in the composition root even though the callback
        // opening a campaign targets a method on a shell that does not exist yet - closes
        // over `_shell` and resolves only on the first click, long after
        // OnFrameworkInitializationCompleted builds the shell.
        _campaignLibrary = new CampaignLibraryViewModel(
            _repositoriesBySystem,
            new CreateCampaign(_repositoriesBySystem, TimeProvider.System),
            _preparations,
            _systems,
            summary => _shell!.OpenCampaignAsync(summary));

        // Explicit array - its order IS execution order. Every visual the GM could first
        // see immediately after system selection warms here, behind the startup
        // curtain, before the selection screen becomes interactive - otherwise system selection
        // would freeze the window: first each compiled-in system prepares its own content (packs,
        // cards - its own startup steps, whose work the shell does not know), then the shelf, data for each
        // shelf campaign, shell chrome (selection screen, shelf, sidebar in both states, campaign
        // page), and finally each compiled-in system's tabs (its content tabs,
        // desk with tools).
        var shelfStep = new LoadCampaignShelfStep(_campaignLibrary);
        var dataStep = new WarmCampaignDataStep(_preparations, shelfStep);

        _startupSteps =
        [
            .. _systems.SelectMany(system => system.StartupSteps),
            shelfStep,
            dataStep,
            new WarmFrameChromeStep(_systems, _campaignLibrary, _preparations, dataStep),
            new WarmSystemTabsStep(_systems, _repositoriesBySystem, _preparations, dataStep)
        ];
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Attached here, not in Program.Main: the dispatcher exists only once Avalonia is up.
            // Only in the desktop lifetime: headless tests run this same App, and there an exception
            // on the UI thread has to keep failing the test instead of becoming a notification.
            Dispatcher.UIThread.UnhandledException += (_, e) => UiThreadErrors.Handle(e, desktop.MainWindow);

            Themes.EditFocusRelease.Register();
            Themes.OpenerHoverRest.Register();
            Themes.PopupOpenMotion.Register();
            Themes.PopupOpenLayout.Register();
            Themes.ExpanderContentMotion.Register();

            _shell = new AppShellViewModel(
                _systems,
                _repositoriesBySystem!,
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
    /// send anything to, so this falls back to a permanent delete.
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
