using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.StatusBar;

public sealed class StatusBarViewModel(string message) : ObservableObject
{
    private string _message = message;
    private double _sidebarWidth = 224;

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => SetField(ref _sidebarWidth, value);
    }
}
