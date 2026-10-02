using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Desktop.Shell.StatusBar;

public sealed partial class StatusBarViewModel(string message) : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; } = message;

    [ObservableProperty]
    public partial double SidebarWidth { get; set; } = 224;
}
