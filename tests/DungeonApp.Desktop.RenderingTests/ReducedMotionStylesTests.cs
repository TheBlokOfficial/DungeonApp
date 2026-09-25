using System;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Style zdejmujące przejścia (Themes/ReducedMotion.axaml) dołącza się tylko na komputerze
/// z wyłączonymi animacjami - błąd w nich nie wyszedłby w aplikacji autora. Ten sam adres, który
/// podaje SystemMotion, daje się wczytać.
/// </summary>
public sealed class ReducedMotionStylesTests
{
    [AvaloniaFact]
    public void Reduced_motion_styles_load_from_the_address_the_frame_uses()
    {
        var styles = (Styles)AvaloniaXamlLoader.Load(
            new Uri("avares://DungeonApp.Desktop/Themes/ReducedMotion.axaml"));

        Assert.Equal(4, styles.Count);
    }
}
