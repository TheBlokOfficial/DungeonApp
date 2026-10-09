using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Systems;

namespace DungeonApp.Desktop.Shell.Sidebars;

/// <summary>
/// The sidebar shown once a system is chosen: three categories - Kampania (the campaign position -
/// the shelf, or the desk once a campaign is open - plus the chosen system's Campaign-category
/// tabs, which stand under the desk), System (the chosen system's System-category
/// tabs, shown on screen as "Biblioteka") and Aplikacja (shown on screen as "System" - "Galeria
/// kontrolek" and "Ustawienia", frame-owned positions). The category names are this class's own row
/// grouping - and, for the middle and last one, deliberately not what the screen shows for them;
/// the shell decides what each selection shows.
/// <para>
/// Owns exactly one piece of navigation state: which row is active. That state spans all three
/// categories together - the campaign position, every tab of either the Kampania or System category,
/// and "Ustawienia" are one mutually exclusive set, because the content area shows exactly one of
/// them at a time. "Ustawienia" is a real destination, not a one-off action: it lingers as a
/// highlighted row exactly like any tab does.
/// </para>
/// </summary>
public sealed partial class GlobalSidebarViewModel : ObservableObject
{
    private const string LockedIconResourceKey = "DungeonIconLock";
    private const string ShelfIconResourceKey = "DungeonIconBookOpen";
    private const string DeskIconResourceKey = "DungeonIconDockBottom";
    // The campaign position's label while no campaign is open, and while one is.
    private const string ShelfLabel = "Kampanie";
    private const string DeskLabel = "Biurko";

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
        Func<Task> selectGallery,
        Func<Task> selectSettings,
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
        // declared order. A bare ContentPresenter for the campaign position draws nothing: the row
        // reserves its height but its DataTemplate resolves against a null Content.
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

        // The controls gallery: a frame-owned destination like Ustawienia,
        // drawn above it in the same category.
        GalleryItem = CreateSelectableItem("shell.gallery", "DungeonIconGallery", "Galeria kontrolek", selectGallery);
        _selectableItems.Add(GalleryItem);
        _allItems.Add(GalleryItem);

        // A real destination: it joins
        // _selectableItems like the campaign position and every tab do, so choosing it lingers as a
        // highlighted row instead of firing a one-off action.
        SettingsItem = CreateSelectableItem("shell.settings", "DungeonIconSettings", "Ustawienia", selectSettings);
        _selectableItems.Add(SettingsItem);
        _allItems.Add(SettingsItem);

        // The Aplikacja category's rows, in display order, for the same ItemsControl mechanism -
        // see CampaignItems above for why a bare ContentPresenter is not used.
        ApplicationItems = [GalleryItem, SettingsItem];

        // Set before this instance is ever handed to a view: Avalonia's
        // Transitions only animate a property change measured against a frame the control already
        // rendered. Setting the target collapse state here, before GlobalSidebarView is even
        // constructed, gives the first layout pass nothing earlier to transition from, so the
        // width/heading/label animations play only on a later real toggle - never on first show, and
        // never on the fresh instance choosing a new system builds.
        if (startCollapsed)
        {
            IsCollapsed = true;
        }

        // Initial highlight: the campaign position, shown as the shelf before any tab is ever clicked.
        Select(CampaignPositionItem);
    }

    /// <summary>The Kampania category's own row - the shelf when no campaign is open, the desk once one is.</summary>
    public NavigationItemViewModel CampaignPositionItem { get; }

    /// <summary>The Kampania category's tab rows, one per <see cref="Systems.IGameSystem.CampaignTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> CampaignTabItems { get; }

    /// <summary>
    /// The Kampania category's whole row list - <see cref="CampaignPositionItem"/> followed by
    /// <see cref="CampaignTabItems"/> - for the one <c>ItemsControl</c> that draws the category.
    /// </summary>
    public IReadOnlyList<NavigationItemViewModel> CampaignItems { get; }

    /// <summary>The System category's rows, one per <see cref="Systems.IGameSystem.SystemTabs"/> entry, in declared order.</summary>
    public IReadOnlyList<NavigationItemViewModel> SystemTabItems { get; }

    /// <summary>The Aplikacja category's "Galeria kontrolek" row - a frame-owned position showing every theme control in every state.</summary>
    public NavigationItemViewModel GalleryItem { get; }

    /// <summary>The Aplikacja category's "Ustawienia" row - a frame-owned position with no content of its own.</summary>
    public NavigationItemViewModel SettingsItem { get; }

    /// <summary>
    /// <see cref="GalleryItem"/> and <see cref="SettingsItem"/>, in display order, as the list the
    /// Aplikacja category's <c>ItemsControl</c> draws - see <see cref="CampaignItems"/> for why.
    /// </summary>
    public IReadOnlyList<NavigationItemViewModel> ApplicationItems { get; }

    /// <summary>
    /// The compact rail deliberately keeps the same 40px icon targets as the expanded navigation,
    /// while returning workspace to the current page.
    /// </summary>
    public bool IsCollapsed
    {
        get => _isCollapsed;
        private set
        {
            if (SetProperty(ref _isCollapsed, value))
            {
                foreach (var item in _allItems)
                {
                    item.IsSidebarCollapsed = value;
                }

                OnPropertyChanged(nameof(SidebarWidth));
                OnPropertyChanged(nameof(SidebarToggleToolTip));
                OnPropertyChanged(nameof(CollapsibleTextOpacity));
                OnPropertyChanged(nameof(CollapsibleTextOffset));
                OnPropertyChanged(nameof(HeadingHeight));
            }
        }
    }

    public double SidebarWidth => IsCollapsed ? 64 : 224;

    public string SidebarToggleToolTip => IsCollapsed ? "Rozwiń panel boczny" : "Zwiń panel boczny";

    public double CollapsibleTextOpacity => IsCollapsed ? 0 : 1;

    public double CollapsibleTextOffset => IsCollapsed ? -12 : 0;

    /// <summary>
    /// Each of the three group headings' own row height - 44. Animated to 0 on collapse
    /// (its Border's Height transition, GlobalSidebarView.axaml) rather than translating the item
    /// list under a still-reserved row, so a group's own Grid actually shrinks and every row below it
    /// - including the next group's heading - reflows in the same motion instead of leaving a gap the
    /// size of that group's own heading behind.
    /// </summary>
    public double HeadingHeight => IsCollapsed ? 0 : 44;

    /// <summary>
    /// Called by the shell on every campaign open and close. Swaps the campaign position's label and
    /// icon between the shelf and the desk, and locks or unlocks every Campaign-category
    /// tab - each one's icon becomes the lock glyph while locked, its own declared icon while not.
    /// </summary>
    public void SetCampaignOpen(bool isOpen)
    {
        CampaignPositionItem.Label = isOpen ? DeskLabel : ShelfLabel;
        CampaignPositionItem.IconResourceKey = isOpen ? DeskIconResourceKey : ShelfIconResourceKey;

        foreach (var (item, unlockedIconResourceKey) in _campaignTabItems)
        {
            item.IsLocked = !isOpen;
            item.IconResourceKey = isOpen ? unlockedIconResourceKey : LockedIconResourceKey;
        }
    }

    /// <summary>Highlights the campaign position row without going through its command - used right after opening a campaign.</summary>
    public void ActivateCampaignPosition() => Select(CampaignPositionItem);

    [RelayCommand]
    private void ToggleCollapsed() => IsCollapsed = !IsCollapsed;

    private NavigationItemViewModel CreateSelectableItem(string id, string iconResourceKey, string label, Func<Task> onSelected)
    {
        NavigationItemViewModel? item = null;
        item = new NavigationItemViewModel(id, iconResourceKey, label, new AsyncRelayCommand(async () =>
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
