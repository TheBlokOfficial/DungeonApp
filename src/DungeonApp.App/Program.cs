using Avalonia;
using System;

namespace DungeonApp.App;

/// <summary>
/// Composition root: the only project allowed to name a content set by name. That is what lets "the
/// shell knows no content set" be checked by a project-reference test rather than by review. See
/// docs/architecture.md, section "Warstwy i granice".
/// </summary>
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DungeonApp.Desktop.App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
