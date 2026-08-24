using Avalonia;
using System;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

class Program
{
    public static UiScaleProfile RequestedUiScaleProfile { get; private set; } = UiScaleProfile.Medium;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        RequestedUiScaleProfile = UiScaleProfiles.Parse(args);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
