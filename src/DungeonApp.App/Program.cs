using Avalonia;
using System;
using System.Collections.Generic;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Desktop.Content;

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
    //
    // Uses the AppBuilder.Configure<TApp>(Func<TApp>) overload rather than the parameterless one,
    // because DungeonApp.Desktop.App needs the compiled-in content set list handed to it through
    // its constructor - the alternative, a static/mutable holder some other code populates before
    // Avalonia touches the app, is exactly the state docs/architecture.md rules out for this list.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new DungeonApp.Desktop.App(BuildContentSets()))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    // The one place in the app that lists content sets by name. Hard-wired by project reference,
    // never discovered at runtime - see docs/architecture.md, "Warstwy i granice", on why loading
    // one from a plugin directory buys nothing here.
    private static IReadOnlyList<IContentSet> BuildContentSets() => [new Dnd5eContentSet()];
}
