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
    private bool _isCollapsed;

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
        ToggleCollapsedCommand = new AsyncCommand(() =>
        {
            IsCollapsed = !IsCollapsed;
            return Task.CompletedTask;
        });
    }

    public IReadOnlyList<NavigationItemViewModel> LibraryItems { get; }

    public AsyncCommand CloseCampaignCommand { get; }

    public AsyncCommand ToggleCollapsedCommand { get; }

    /// <summary>
    /// The compact rail deliberately keeps the same 40px icon targets as the expanded navigation,
    /// while returning workspace to the current page.
    /// </summary>
    public bool IsCollapsed
    {
        get => _isCollapsed;
        private set
        {
            if (SetField(ref _isCollapsed, value))
            {
                foreach (var item in _allItems)
                {
                    item.IsSidebarCollapsed = value;
                }

                RaisePropertyChanged(nameof(SidebarWidth));
                RaisePropertyChanged(nameof(SidebarToggleToolTip));
            }
        }
    }

    public double SidebarWidth => IsCollapsed ? 64 : 224;

    public string SidebarToggleToolTip => IsCollapsed ? "Rozwiń panel boczny" : "Zwiń panel boczny";

    public double CollapsibleTextOpacity => IsCollapsed ? 0 : 1;

    public double CollapsibleTextOffset => IsCollapsed ? -12 : 0;

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
