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
/// <see cref="AvaloniaHeadlessPlatformOptions.UseHeadlessDrawing"/> is set to <c>false</c>: the
/// default (<c>true</c>) stub renderer runs layout and styling in full but never actually rasterizes
/// anything, so a captured frame comes back fully transparent - measured directly, by writing a pixel
/// -sampling assertion against the default and watching it read back <c>#00000000</c> regardless of
/// what the control underneath actually painted. With it off, layout, styling and opacity assertions
/// (every other test in this project) behave identically, and a <see cref="RenderTargetBitmap"/> of a
/// control now reads back the real rendered color - the only way to sample a background pixel at all
/// (CampaignLibraryRenderingTests's own background test).
/// </para>
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure(() => new App([new EmptyGameSystem()]))
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
