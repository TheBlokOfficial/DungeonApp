using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Shell.StatusBar;

public sealed class StatusBarViewModel(string message) : ObservableObject
{
    private string _message = message;

    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }
}
