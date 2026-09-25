using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Pojawienie się elementu warstwy okna (okno potwierdzenia, dymek powiadomienia): wyłania się
/// z przezroczystości i dosuwa o podane przesunięcie w DungeonPopupOpenDuration, z wyhamowaniem.
/// Element jest klikalny od pierwszej klatki - ruch tylko dogania stan; zniknięcie jest natychmiastowe.
/// Przy wyłączonych animacjach w systemie (<see cref="Themes.SystemMotion.IsReduced"/>) nic się nie
/// rusza. Zmienia wyłącznie wygląd widoku.
/// </summary>
internal static class OverlayAppearMotion
{
    public static void Run(Control fading, Control sliding, double fromX, double fromY)
    {
        if (Themes.SystemMotion.IsReduced)
        {
            return;
        }

        var duration = fading.TryFindResource("DungeonPopupOpenDuration", out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(120);

        // Animator przekształceń Avalonii 12 animuje TranslateTransform, nie TransformOperations.
        if (sliding.RenderTransform is not TranslateTransform)
        {
            sliding.RenderTransform = new TranslateTransform();
        }

        _ = Animate(duration, Visual.OpacityProperty, 0d, 1d).RunAsync(fading);
        _ = Animate(duration, TranslateTransform.XProperty, fromX, 0d).RunAsync(sliding);
        _ = Animate(duration, TranslateTransform.YProperty, fromY, 0d).RunAsync(sliding);
    }

    private static Animation Animate(TimeSpan duration, AvaloniaProperty property, double from, double to)
    {
        return new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(property, from) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(property, to) } },
            },
        };
    }
}
