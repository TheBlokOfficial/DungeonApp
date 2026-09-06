using System.Windows.Input;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class NavigationItemViewModel(string id, string iconResourceKey, string label, ICommand selectCommand)
    : ObservableObject
{
    private bool _isActive;
    private bool _isSidebarCollapsed;

    public string Id { get; } = id;

    public string IconResourceKey { get; } = iconResourceKey;

    public string Label { get; } = label;

    public ICommand SelectCommand { get; } = selectCommand;

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

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
