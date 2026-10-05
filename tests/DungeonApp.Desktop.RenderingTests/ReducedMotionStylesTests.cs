using System;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Styles removing transitions (Themes/ReducedMotion.axaml) are included only on computers
/// with animations disabled, so errors can go unnoticed in the author's application. The same URI
/// supplied by SystemMotion must load successfully.
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
