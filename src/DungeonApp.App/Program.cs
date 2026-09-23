using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Desktop.Content;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Layout;

namespace DungeonApp.App;

/// <summary>
/// Composition root: the only project allowed to name a system by name. That is what lets "the
/// shell knows no system" be checked by a project-reference test rather than by review. See
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
    //
    // Uses the AppBuilder.Configure<TApp>(Func<TApp>) overload rather than the parameterless one,
    // because DungeonApp.Desktop.App needs the compiled-in system list handed to it through
    // its constructor - the alternative, a static/mutable holder some other code populates before
    // Avalonia touches the app, is exactly the state docs/architecture.md rules out for this list.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new DungeonApp.Desktop.App(BuildSystems()))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    // The one place in the app that lists systems by name. Hard-wired by project reference,
    // never discovered at runtime - see docs/architecture.md, "Warstwy i granice", on why loading
    // one from a plugin directory buys nothing here.
    //
    // Also the one place that computes the desk layout store's path and hands it to the system in
    // its constructor (docs/tasks.md, etap 1: "system dostaje w konstruktorze gołe
    // WorkspaceLayoutStore, a wyliczenie katalogu danych aplikacji przenosi się w całości do
    // Program.cs") - DungeonApp.Desktop's own composition root (App.axaml.cs) no longer knows this
    // path, or the store type, at all.
    private static IReadOnlyList<IGameSystem> BuildSystems()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonApp");

        var layoutStore = new WorkspaceLayoutStore(appDataDirectory);

        return [new Dnd5eSystem(layoutStore)];
    }
}
