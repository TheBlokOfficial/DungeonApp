using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Wypełnienie nieokreślonego wskaźnika postępu (część szablonu ProgressBar, Themes/DungeonControls.axaml):
/// odcinek szerokości <see cref="SegmentFraction"/> toru przesuwa się w pętli od lewej do prawej
/// w czasie DungeonProgressSweepDuration. To ruch ciągły, nie przejście stanu - rysuje go kod, bo
/// animacja stylu nie daje się wyłączyć stylem ReducedMotion.axaml.
/// Spokojny (bez ruchu, cały tor w <see cref="CalmFill"/>): przy wyłączonych animacjach w systemie
/// (<see cref="Themes.SystemMotion.IsReduced"/>) i gdy kontrolka jest wyłączona.
/// Zmienia wyłącznie wygląd, nigdy stan aplikacji.
/// </summary>
public sealed class ProgressSweep : Control
{
    /// <summary>Szerokość odcinka jako część szerokości toru.</summary>
    public const double SegmentFraction = 0.4;

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressSweep, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> CalmFillProperty =
        AvaloniaProperty.Register<ProgressSweep, IBrush?>(nameof(CalmFill));

    private static readonly CubicEaseInOut Easing = new();

    private readonly Stopwatch clock = new();
    private DispatcherTimer? timer;

    static ProgressSweep()
    {
        AffectsRender<ProgressSweep>(FillProperty, CalmFillProperty);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? CalmFill
    {
        get => GetValue(CalmFillProperty);
        set => SetValue(CalmFillProperty, value);
    }

    private bool IsCalm => Themes.SystemMotion.IsReduced || !IsEffectivelyEnabled;

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (IsCalm)
        {
            if (CalmFill is { } calm)
            {
                context.FillRectangle(calm, bounds);
            }

            return;
        }

        if (Fill is not { } fill)
        {
            return;
        }

        var period = this.TryFindResource("DungeonProgressSweepDuration", out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromSeconds(1.6);
        var progress = clock.Elapsed.TotalMilliseconds % period.TotalMilliseconds / period.TotalMilliseconds;
        var segment = bounds.Width * SegmentFraction;
        var left = -segment + (Easing.Ease(progress) * (bounds.Width + segment));
        context.FillRectangle(fill, new Rect(left, 0, segment, bounds.Height));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopTimer();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            UpdateTimer();
            InvalidateVisual();
        }
    }

    private void UpdateTimer()
    {
        if (IsCalm || !IsVisible || VisualRoot is null)
        {
            StopTimer();
            return;
        }

        if (timer is null)
        {
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnTick);
            clock.Restart();
            timer.Start();
        }
    }

    private void StopTimer()
    {
        timer?.Stop();
        timer = null;
        clock.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (IsEffectivelyVisible)
        {
            InvalidateVisual();
        }
    }
}
