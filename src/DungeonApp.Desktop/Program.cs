using Avalonia;
using System;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop;

class Program
{
    /// <summary>One-run override from <c>--ui-scale=</c>; null means "use the persisted setting".</summary>
    public static UiScaleProfile? UiScaleProfileOverride { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        UiScaleProfileOverride = UiScaleProfiles.Parse(args);
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
