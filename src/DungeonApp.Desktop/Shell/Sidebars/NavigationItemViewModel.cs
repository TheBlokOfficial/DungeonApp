using System.Windows.Input;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.Sidebars;

public sealed class NavigationItemViewModel(string id, string iconResourceKey, string label, ICommand selectCommand)
    : ObservableObject
{
    private bool _isActive;

    public string Id { get; } = id;

    public string IconResourceKey { get; } = iconResourceKey;

    public string Label { get; } = label;

    public ICommand SelectCommand { get; } = selectCommand;

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }
}
