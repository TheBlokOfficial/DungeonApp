using System.Windows.Input;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

/// <summary>
/// One row on the global sidebar - a fixed id and command, plus a label and icon that a caller can
/// still update afterwards. Two rows need that: the campaign position (shelf vs. campaign page swap
/// both, docs/tasks.md's "ikona pozycji paska staje się stanem pochodnym") and a Campaign-category
/// tab (its icon becomes the lock glyph while no campaign is open).
/// </summary>
public sealed class NavigationItemViewModel(string id, string iconResourceKey, string label, ICommand selectCommand)
    : ObservableObject
{
    private string _iconResourceKey = iconResourceKey;
    private string _label = label;
    private bool _isActive;
    private bool _isSidebarCollapsed;
    private bool _isLocked;

    public string Id { get; } = id;

    public string IconResourceKey
    {
        get => _iconResourceKey;
        set => SetField(ref _iconResourceKey, value);
    }

    public string Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    public ICommand SelectCommand { get; } = selectCommand;

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    /// <summary>
    /// True for a Campaign-category tab while no campaign is open. A locked row is greyed out,
    /// shows the lock icon in place of its own, and does not react to a click - the shell never even
    /// builds its content while this is true (docs/architecture.md, "Zakładki kampanii są na pasku
    /// od wyboru systemu; bez otwartej kampanii są zamknięte").
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetField(ref _isLocked, value))
            {
                RaisePropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public bool IsEnabled => !IsLocked;

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (SetField(ref _isSidebarCollapsed, value))
            {
                RaisePropertyChanged(nameof(LabelOpacity));
                RaisePropertyChanged(nameof(LabelOffset));
            }
        }
    }

    public double LabelOpacity => IsSidebarCollapsed ? 0 : 1;

    public double LabelOffset => IsSidebarCollapsed ? -12 : 0;
}
