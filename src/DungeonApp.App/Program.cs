using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.App;

/// <summary>
/// Composition root: the only project allowed to name a system by name. That is what lets a
/// project-reference test, rather than review, check that the shell knows no system.
/// </summary>
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    //
    // The log is set up first, before Avalonia, so a failure anywhere after this line leaves a trace.
    // These handlers see failures off the UI thread and only record them: a crash on a background
    // thread still ends the process exactly as it would without them. The UI thread's own handler
    // keeps the program running; it needs a running dispatcher and is attached in App instead.
    [STAThread]
    public static void Main(string[] args)
    {
        var log = new FileLog(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DungeonApp", "logs"));
        AppLog.Use(log.Write);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLog.Error("Nieobsłużony wyjątek, program kończy działanie.", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            AppLog.Error("Nieobserwowany wyjątek zadania w tle.", e.Exception);

        AppLog.Info("Start programu.");

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            AppLog.Error("Awaria programu.", ex);
            throw;
        }

        AppLog.Info("Koniec programu.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    //
    // Uses the AppBuilder.Configure<TApp>(Func<TApp>) overload rather than the parameterless one,
    // because DungeonApp.Desktop.App needs the compiled-in system list handed to it through
    // its constructor - the alternative, a static/mutable holder some other code populates before
    // Avalonia touches the app, is exactly the hidden global state this list must not live in.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new DungeonApp.Desktop.App(BuildSystems()))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    // The one place in the app that lists systems by name. Hard-wired by project reference,
    // never discovered at runtime, because loading one from a plugin directory would buy nothing
    // here.
    //
    // Also the one place that computes the desk layout store's path and the content packs' path and
    // hands both to the system in its constructor, so that DungeonApp.Desktop's own composition
    // root (App.axaml.cs) knows neither path, nor either type, at all.
    private static IReadOnlyList<IGameSystem> BuildSystems()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");

        var layoutStore = new WorkspaceLayoutStore(appDataDirectory);

        // Content packs are user documents just like campaigns, stored alongside them rather than
        // under application data. A pack belongs to the system whose directory contains it:
        // SystemDirectories computes the shared campaign and pack layout from the id this system
        // presents to the frame (Dnd5eSystem.IdValue), avoiding a second literal "dnd5e" here.
        //
        // The other source is bundled packs: the same <system>\packs\ layout, read-only under the
        // program directory (AppContext.BaseDirectory). The system receives both paths as one
        // list and scans them into one registry without knowing which source each comes from.
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var dnd5e = SystemId.Create(Dnd5eSystem.IdValue);
        string[] packsPaths =
        [
            SystemDirectories.GmPacks(documentsPath, dnd5e),
            SystemDirectories.BundledPacks(AppContext.BaseDirectory, dnd5e),
        ];

        return [new Dnd5eSystem(layoutStore, packsPaths)];
    }
}
