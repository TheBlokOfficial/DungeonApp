using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class GlobalSidebarViewModel : ObservableObject
{
    private readonly Action<NavigationItemViewModel> _onSelected;
    private readonly IReadOnlyList<NavigationItemViewModel> _allItems;
    private bool _isCampaignOpen;

    public GlobalSidebarViewModel(
        Action<NavigationItemViewModel> onSelected,
        Func<Task> closeCampaign)
    {
        _onSelected = onSelected;

        LibraryItems =
        [
            CreateItem("campaigns", "DungeonIconBookOpen", "Kampanie")
        ];
        _allItems = LibraryItems;

        LibraryItems[0].IsActive = true;
        CloseCampaignCommand = new AsyncCommand(closeCampaign, () => IsCampaignOpen);
    }

    public IReadOnlyList<NavigationItemViewModel> LibraryItems { get; }

    public AsyncCommand CloseCampaignCommand { get; }

    /// <summary>
    /// The campaign workspace has no global top bar. Its route back to the library therefore lives
    /// in this global rail, in the same position as the mockup's "Biblioteka kampanii" affordance.
    /// </summary>
    public bool IsCampaignOpen
    {
        get => _isCampaignOpen;
        set
        {
            if (SetField(ref _isCampaignOpen, value))
            {
                CloseCampaignCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
