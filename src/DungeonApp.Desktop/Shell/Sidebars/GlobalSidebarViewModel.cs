using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

/// <summary>
/// The sidebar shown once a system is chosen: three categories (docs/architecture.md, "Pasek boczny:
/// trzy kategorie") - Kampania (the campaign position, plus the chosen system's Campaign-category
/// tabs), System (the chosen system's System-category tabs) and Aplikacja (today, just "Zmień
/// system"). The category names are this class's own row grouping; the shell decides what each
/// selection shows.
/// <para>
/// Owns exactly one piece of navigation state: which row is active. That state spans the Kampania
/// and System rows together - the campaign position and every tab, of either category, are one
/// mutually exclusive set, because the content area shows exactly one of them at a time. "Zmień
/// system" is not part of that set: choosing it does not linger as a highlighted row, it leaves the
/// system behind entirely.
/// </para>
/// </summary>
public sealed class GlobalSidebarViewModel : ObservableObject
{
    private const string LockedIconResourceKey = "DungeonIconLock";
    private const string ShelfIconResourceKey = "DungeonIconBookOpen";
    private const string CampaignPageIconResourceKey = "DungeonIconCampaignRecord";
    private const string ShelfLabel = "Biblioteka kampanii";

    private readonly List<NavigationItemViewModel> _selectableItems = [];
    private readonly List<NavigationItemViewModel> _allItems = [];
    private readonly List<(NavigationItemViewModel Item, string UnlockedIconResourceKey)> _campaignTabItems = [];

    private bool _isCollapsed;

    public GlobalSidebarViewModel(
        IReadOnlyList<SystemTabDeclaration> systemTabs,
        IReadOnlyList<CampaignTabDeclaration> campaignTabs,
        Func<Task> selectCampaignPosition,
        Func<CampaignTabDeclaration, Task> selectCampaignTab,
        Action<SystemTabDeclaration> selectSystemTab,
        Func<Task> changeSystem)
    {
        CampaignPositionItem = CreateSelectableItem(
            "shell.campaign-position", ShelfIconResourceKey, ShelfLabel, selectCampaignPosition);
        _selectableItems.Add(CampaignPositionItem);
        _allItems.Add(CampaignPositionItem);

        CampaignTabItems =
        [
            .. campaignTabs.Select(declaration =>
            {
                var item = CreateSelectableItem(
                    declaration.Id, LockedIconResourceKey, declaration.Title, () => selectCampaignTab(declaration));
                item.IsLocked = true;

                _campaignTabItems.Add((item, declaration.IconResourceKey));
                _selectableItems.Add(item);
                _allItems.Add(item);

                return item;
            }),
        ];

        SystemTabItems =
        [
            .. systemTabs.Select(declaration =>
            {
                var item = CreateSelectableItem(
                    declaration.Id, declaration.IconResourceKey, declaration.Title,
                    () =>
                    {
                        selectSystemTab(declaration);
                        return Task.CompletedTask;
                    });

                _selectableItems.Add(item);
                _allItems.Add(item);

                return item;
            }),
        ];

        // Never part of _selectableItems: leaving the system is a momentary action, not a
        // destination that stays highlighted the way a tab does.
        ChangeSystemItem = new NavigationItemViewModel(
            "shell.change-system", "DungeonIconSettings", "Zmień system", new AsyncCommand(changeSystem));
        _allItems.Add(ChangeSystemItem);

        ToggleCollapsedCommand = new AsyncCommand(() =>
        {
            IsCollapsed = !IsCollapsed;
            return Task.CompletedTask;
        });

        // Initial highlight: the campaign position, shown as the shelf before any tab is ever clicked.
        Select(CampaignPositionItem);
    }

    /// <summary>The Kampania category's own row - the shelf when no campaign is open, the campaign page once one is.</summary>
    public NavigationItemViewModel CampaignPositionItem { get; }

    /// <summary>The Kampania category's tab rows, one per <see cref="Content.IGameSystem.CampaignTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> CampaignTabItems { get; }

    /// <summary>The System category's rows, one per <see cref="Content.IGameSystem.SystemTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> SystemTabItems { get; }

    /// <summary>The Aplikacja category's one row today.</summary>
    public NavigationItemViewModel ChangeSystemItem { get; }

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
                RaisePropertyChanged(nameof(CollapsibleTextOpacity));
                RaisePropertyChanged(nameof(CollapsibleTextOffset));
                RaisePropertyChanged(nameof(NavigationItemsOffset));
            }
        }
    }

    public double SidebarWidth => IsCollapsed ? 64 : 224;

    public string SidebarToggleToolTip => IsCollapsed ? "Rozwiń panel boczny" : "Zwiń panel boczny";

    public double CollapsibleTextOpacity => IsCollapsed ? 0 : 1;

    public double CollapsibleTextOffset => IsCollapsed ? -12 : 0;

    public double NavigationItemsOffset => IsCollapsed ? -44 : 0;

    /// <summary>
    /// Called by the shell on every campaign open and close. Swaps the campaign position's label and
    /// icon between the shelf and the campaign page, and locks or unlocks every Campaign-category
    /// tab - each one's icon becomes the lock glyph while locked, its own declared icon while not
    /// (docs/tasks.md, etap 1: "kłódka zamiast ikony zakładki").
    /// </summary>
    public void SetCampaignOpen(bool isOpen, string? campaignName)
    {
        CampaignPositionItem.Label = isOpen ? campaignName ?? ShelfLabel : ShelfLabel;
        CampaignPositionItem.IconResourceKey = isOpen ? CampaignPageIconResourceKey : ShelfIconResourceKey;

        foreach (var (item, unlockedIconResourceKey) in _campaignTabItems)
        {
            item.IsLocked = !isOpen;
            item.IconResourceKey = isOpen ? unlockedIconResourceKey : LockedIconResourceKey;
        }
    }

    /// <summary>Highlights the campaign position row without going through its command - used right after opening a campaign.</summary>
    public void ActivateCampaignPosition() => Select(CampaignPositionItem);

    private NavigationItemViewModel CreateSelectableItem(string id, string iconResourceKey, string label, Func<Task> onSelected)
    {
        NavigationItemViewModel? item = null;
        item = new NavigationItemViewModel(id, iconResourceKey, label, new AsyncCommand(async () =>
        {
            if (item!.IsLocked)
            {
                return;
            }

            Select(item);
            await onSelected();
        }));

        return item;
    }

    private void Select(NavigationItemViewModel selected)
    {
        foreach (var item in _selectableItems)
        {
            item.IsActive = item == selected;
        }
    }
}
