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
    // The pre-etap-1 sidebar's shelf row used this exact label (555802f, GlobalSidebarViewModel's
    // "campaigns" item) - the campaign position keeps it while no campaign is open.
    private const string ShelfLabel = "Kampanie";

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
        Func<Task> changeSystem,
        bool startCollapsed = false)
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

        // The Kampania category's row, drawn by one ItemsControl - the campaign position first,
        // its own row's identity never changes with it, then the system's campaign tabs in
        // declared order. A bare ContentPresenter for the campaign position, tried first, drew
        // nothing (docs/tasks.md: the row reserved its height but its DataTemplate resolved
        // against a null Content) - the fix is the same mechanism every other row already used.
        CampaignItems = [CampaignPositionItem, .. CampaignTabItems];

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

        // The Aplikacja category's row, as the one-element collection the same ItemsControl
        // mechanism needs - see CampaignItems above for why a bare ContentPresenter is not used.
        ChangeSystemItems = [ChangeSystemItem];

        ToggleCollapsedCommand = new AsyncCommand(() =>
        {
            IsCollapsed = !IsCollapsed;
            return Task.CompletedTask;
        });

        // Set before this instance is ever handed to a view (docs/tasks.md, zadanie 2): Avalonia's
        // Transitions only animate a property change measured against a frame the control already
        // rendered. Setting the target collapse state here, before GlobalSidebarView is even
        // constructed, gives the first layout pass nothing earlier to transition from, so the
        // width/heading/label animations play only on a later real toggle - never on first show, and
        // never on the fresh instance "Zmień system" builds for the next system.
        if (startCollapsed)
        {
            IsCollapsed = true;
        }

        // Initial highlight: the campaign position, shown as the shelf before any tab is ever clicked.
        Select(CampaignPositionItem);
    }

    /// <summary>The Kampania category's own row - the shelf when no campaign is open, the campaign page once one is.</summary>
    public NavigationItemViewModel CampaignPositionItem { get; }

    /// <summary>The Kampania category's tab rows, one per <see cref="Content.IGameSystem.CampaignTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> CampaignTabItems { get; }

    /// <summary>
    /// The Kampania category's whole row list - <see cref="CampaignPositionItem"/> followed by
    /// <see cref="CampaignTabItems"/> - for the one <c>ItemsControl</c> that draws the category
    /// (docs/architecture.md, "Pasek boczny: trzy kategorie").
    /// </summary>
    public IReadOnlyList<NavigationItemViewModel> CampaignItems { get; }

    /// <summary>The System category's rows, one per <see cref="Content.IGameSystem.SystemTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> SystemTabItems { get; }

    /// <summary>The Aplikacja category's one row today.</summary>
    public NavigationItemViewModel ChangeSystemItem { get; }

    /// <summary>
    /// <see cref="ChangeSystemItem"/> as the one-element list the Aplikacja category's
    /// <c>ItemsControl</c> draws - see <see cref="CampaignItems"/> for why.
    /// </summary>
    public IReadOnlyList<NavigationItemViewModel> ChangeSystemItems { get; }

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
                RaisePropertyChanged(nameof(HeadingHeight));
            }
        }
    }

    public double SidebarWidth => IsCollapsed ? 64 : 224;

    public string SidebarToggleToolTip => IsCollapsed ? "Rozwiń panel boczny" : "Zwiń panel boczny";

    public double CollapsibleTextOpacity => IsCollapsed ? 0 : 1;

    public double CollapsibleTextOffset => IsCollapsed ? -12 : 0;

    /// <summary>
    /// Each of the three group headings' own row height - 44 unchanged from the value this sidebar
    /// already used for a heading row (docs/decisions.md, "Nawigacja"). Animated to 0 on collapse
    /// (its Border's Height transition, GlobalSidebarView.axaml) rather than translating the item
    /// list under a still-reserved row, so a group's own Grid actually shrinks and every row below it
    /// - including the next group's heading - reflows in the same motion instead of leaving a gap the
    /// size of that group's own heading behind.
    /// </summary>
    public double HeadingHeight => IsCollapsed ? 0 : 44;

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
