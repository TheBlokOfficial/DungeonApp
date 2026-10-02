using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Shell.StatusBar;

public sealed class StatusBarViewModel(string message) : ObservableObject
{
    private string _message = message;
    private double _sidebarWidth = 224;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => SetProperty(ref _sidebarWidth, value);
    }
}
