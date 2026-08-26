using System;
using DungeonApp.Desktop.Settings;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Shell.StatusBar;
using DungeonApp.Desktop.Shell.TopBar;

namespace DungeonApp.Desktop.Shell;

public sealed class AppShellViewModel
{
    public AppShellViewModel(AppSettings settings, Action<AppSettings> applyAndPersistSettings)
    {
        TopBar = new TopBarViewModel("DungeonApp");
        Sidebar = new GlobalSidebarViewModel(settings, applyAndPersistSettings);
        StatusBar = new StatusBarViewModel("Gotowe");
    }

    public TopBarViewModel TopBar { get; }

    public GlobalSidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }
}
