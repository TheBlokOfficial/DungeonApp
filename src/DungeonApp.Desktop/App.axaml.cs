using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    private WorkspaceLayoutStore? _layoutStore;
    private ModuleCatalog? _modules;
    private JsonCampaignRepository? _campaigns;
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
            .Register(SchedulerModule.Id, () => new SchedulerModule());

        // The campaign library lives with the user's documents, not in application data: a campaign
        // is meant to be a visible, portable, backup-able document rather than hidden app state.
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DungeonApp",
            "Campaigns");

        // A separate store from the campaign's own: the chronicle is append-only, grows without
        // limit, and losing it must never cost the campaign.
        ICampaignJournalStore journal = new JsonCampaignJournalStore(libraryPath);

        _campaigns = new JsonCampaignRepository(libraryPath, _modules, journal, TimeProvider.System);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _shell = new AppShellViewModel(
                _layoutStore!,
                _campaigns!,
                new CreateCampaign(_campaigns!, _modules!, TimeProvider.System));

            desktop.MainWindow = new MainWindow
            {
                DataContext = _shell
            };

            // The last reliable moment to write a pending desk arrangement. Exit does not run on a
            // hard kill, so this is where the debounced layout writer is flushed.
            desktop.ShutdownRequested += (_, _) => _shell?.FlushPendingState();
            desktop.MainWindow.Closing += (_, _) => _shell?.FlushPendingState();

            // Reading the shelf must not hold up the first frame; the library shows its own
            // loading and empty states while this runs.
            _ = _shell.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
