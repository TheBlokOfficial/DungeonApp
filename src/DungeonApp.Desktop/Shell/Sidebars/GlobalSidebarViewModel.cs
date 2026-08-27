using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class GlobalSidebarViewModel : ObservableObject
{
    private readonly Action<NavigationItemViewModel> _onSelected;
    private readonly IReadOnlyList<NavigationItemViewModel> _allItems;

    public GlobalSidebarViewModel(Action<NavigationItemViewModel> onSelected)
    {
        _onSelected = onSelected;

        LibraryItems =
        [
            CreateItem("campaigns", "DungeonIconBookOpen", "Kampanie"),
            CreateItem("characters", "DungeonIconUsers", "Bohaterowie"),
            CreateItem("content-packs", "DungeonIconBoxes", "Paczki zawartości")
        ];
        SystemItems = [CreateItem("settings", "DungeonIconSettings", "Ustawienia")];
        _allItems = [.. LibraryItems, .. SystemItems];

        LibraryItems[0].IsActive = true;
    }

    public IReadOnlyList<NavigationItemViewModel> LibraryItems { get; }

    public IReadOnlyList<NavigationItemViewModel> SystemItems { get; }

    private NavigationItemViewModel CreateItem(string id, string iconResourceKey, string label)
    {
        NavigationItemViewModel? item = null;
        item = new NavigationItemViewModel(id, iconResourceKey, label, new AsyncCommand(() =>
        {
            Select(item!);
            return Task.CompletedTask;
        }));
        return item;
    }

    private void Select(NavigationItemViewModel selected)
    {
        if (selected.IsActive)
        {
            return;
        }

        foreach (var item in _allItems)
        {
            item.IsActive = item == selected;
        }

        _onSelected(selected);
    }
}
