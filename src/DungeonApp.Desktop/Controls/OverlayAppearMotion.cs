using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Window-overlay element appearance (confirmation dialog, notification toast): fades in
/// and slides by the supplied offset over DungeonPopupOpenDuration, easing out.
/// Element is clickable from the first frame - motion only catches up with state; dismissal is immediate.
/// With system animations disabled (<see cref="Themes.SystemMotion.IsReduced"/>), nothing
/// moves. Changes view appearance only.
/// </summary>
internal static class OverlayAppearMotion
{
    public static void Run(Control fading, Control sliding, double fromX, double fromY, string durationKey = "DungeonPopupOpenDuration")
    {
        if (Themes.SystemMotion.IsReduced)
        {
            return;
        }

        var duration = fading.TryFindResource(durationKey, out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(120);

        // Avalonia 12's transform animator animates TranslateTransform, not TransformOperations.
        if (sliding.RenderTransform is not TranslateTransform slide)
        {
            slide = new TranslateTransform();
            sliding.RenderTransform = slide;
        }

        // The start pose is set before the first animation tick: a frame drawn in the final pose before
        // the motion begins reads as the element jumping back to its start.
        fading.Opacity = 0d;
        slide.X = fromX;
        slide.Y = fromY;

        var tasks = new[]
        {
            Animate(duration, Visual.OpacityProperty, 0d, 1d).RunAsync(fading),
            Animate(duration, TranslateTransform.XProperty, fromX, 0d).RunAsync(sliding),
            Animate(duration, TranslateTransform.YProperty, fromY, 0d).RunAsync(sliding),
        };

        _ = SettleAsync(tasks, fading, slide);
    }

    private static async Task SettleAsync(Task[] tasks, Control fading, TranslateTransform slide)
    {
        await Task.WhenAll(tasks);
        fading.ClearValue(Visual.OpacityProperty);
        slide.X = 0d;
        slide.Y = 0d;
    }

    private static Animation Animate(TimeSpan duration, AvaloniaProperty property, double from, double to)
    {
        return new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(property, to) } },
            },
        };
    }
}
