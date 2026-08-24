using System.Collections.Generic;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class GlobalSidebarViewModel
{
    private GlobalSidebarViewModel(
        IReadOnlyList<NavigationItemViewModel> libraryItems,
        NavigationItemViewModel settingsItem)
    {
        LibraryItems = libraryItems;
        SettingsItem = settingsItem;
    }

    public IReadOnlyList<NavigationItemViewModel> LibraryItems { get; }

    public NavigationItemViewModel SettingsItem { get; }

    public static GlobalSidebarViewModel CreateDefault() => new(
        [
            new("DungeonIconBookOpen", "Kampanie", true),
            new("DungeonIconUsers", "Bohaterowie"),
            new("DungeonIconBoxes", "Paczki zawartości")
        ],
        new NavigationItemViewModel("DungeonIconSettings", "Ustawienia"));
}
