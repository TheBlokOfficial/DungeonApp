using Avalonia;
using Avalonia.Headless;
using DungeonApp.Desktop.RenderingTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Wires every headless-rendering test in this assembly to the real <see cref="App"/> (not a stand-in),
/// so styles and <c>DynamicResource</c> tokens from <c>Themes/*</c> resolve exactly as they do in the
/// shipped app (brief: "użyj prawdziwego App z DungeonApp.Desktop, jeśli się da"). No test here goes
/// through <see cref="EmptyGameSystem"/>'s tabs or content types - it exists solely to satisfy
/// <c>App.Initialize()</c>'s "at least one system" guard; each test builds its own
/// <c>GlobalSidebarViewModel</c> directly.
/// <para>
/// Default headless options: layout and styling work, nothing is rasterized to an offscreen bitmap.
/// </para>
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App([new EmptyGameSystem()]))
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
