using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

public partial class App : Avalonia.Application
{
    private AppSettingsStore? _settingsStore;
    private AppSettings? _settings;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");
        _settingsStore = new AppSettingsStore(appDataDirectory);
        var loaded = _settingsStore.Load();
        _settings = Program.UiScaleProfileOverride is { } overrideProfile
            ? loaded with { ScaleProfile = overrideProfile }
            : loaded;

        UiScaleProfiles.Apply(this, _settings.ScaleProfile, _settings.SidebarVariant);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsStore = _settingsStore ?? throw new InvalidOperationException("Initialize() must run before OnFrameworkInitializationCompleted().");
            var settings = _settings ?? throw new InvalidOperationException("Initialize() must run before OnFrameworkInitializationCompleted().");

            void ApplyAndPersist(AppSettings updated)
            {
                UiScaleProfiles.Apply(this, updated.ScaleProfile, updated.SidebarVariant);
                settingsStore.Save(updated);
            }

            var viewModel = new AppShellViewModel(settings, ApplyAndPersist);

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
