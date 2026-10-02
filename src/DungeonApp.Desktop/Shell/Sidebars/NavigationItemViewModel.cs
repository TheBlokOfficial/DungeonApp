using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Shell.Sidebars;

/// <summary>
/// One row on the global sidebar - a fixed id and command, plus a label and icon that a caller can
/// still update afterwards. Two rows need that: the campaign position (shelf vs. campaign page swap
/// both, so its icon is derived state) and a Campaign-category tab (its icon becomes the lock glyph
/// while no campaign is open).
/// </summary>
public sealed partial class NavigationItemViewModel(string id, string iconResourceKey, string label, ICommand selectCommand)
    : ObservableObject
{
    public string Id { get; } = id;

    [ObservableProperty]
    public partial string IconResourceKey { get; set; } = iconResourceKey;

    [ObservableProperty]
    public partial string Label { get; set; } = label;

    public ICommand SelectCommand { get; } = selectCommand;

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>
    /// True for a Campaign-category tab while no campaign is open. A locked row is greyed out,
    /// shows the lock icon in place of its own, and does not react to a click - the shell never even
    /// builds its content while this is true. Campaign tabs sit on the sidebar from the moment a
    /// system is chosen; without an open campaign they are locked.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    public partial bool IsLocked { get; set; }

    public bool IsEnabled => !IsLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelOpacity))]
    [NotifyPropertyChangedFor(nameof(LabelOffset))]
    public partial bool IsSidebarCollapsed { get; set; }

    public double LabelOpacity => IsSidebarCollapsed ? 0 : 1;

    public double LabelOffset => IsSidebarCollapsed ? -12 : 0;
}
