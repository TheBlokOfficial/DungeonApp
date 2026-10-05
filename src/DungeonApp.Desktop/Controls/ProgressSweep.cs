using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Indeterminate progress fill (part of the ProgressBar template, Themes/Controls/ProgressBar.axaml):
/// segment spanning <see cref="SegmentFraction"/> of the track loops from left to right
/// over DungeonProgressSweepDuration. Continuous motion, not a state transition - drawn in code because
/// a style animation cannot be disabled through ReducedMotion.axaml. The window's rendering loop drives
/// frames (<see cref="TopLevel.RequestAnimationFrame"/>) - updates follow the display refresh rate, not
/// a fixed-interval timer; position uses elapsed time, independent of frame count.
/// Calm (no motion, entire track in <see cref="CalmFill"/>): when system animations are disabled
/// (<see cref="Themes.SystemMotion.IsReduced"/>) or the control is disabled.
/// Changes appearance only, never application state.
/// </summary>
public sealed class ProgressSweep : Control
{
    /// <summary>Segment width as a fraction of track width.</summary>
    public const double SegmentFraction = 0.4;

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressSweep, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> CalmFillProperty =
        AvaloniaProperty.Register<ProgressSweep, IBrush?>(nameof(CalmFill));

    private static readonly CubicEaseInOut Easing = new();

    private readonly Stopwatch clock = new();
    private bool running;
    private bool frameRequested;

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
        UpdateMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopMotion();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            UpdateMotion();
            InvalidateVisual();
        }
    }

    private void UpdateMotion()
    {
        if (IsCalm || !IsVisible || VisualRoot is null)
        {
            StopMotion();
            return;
        }

        if (!running)
        {
            running = true;
            clock.Restart();
            RequestFrame();
        }
    }

    private void StopMotion()
    {
        running = false;
        clock.Stop();
    }

    private void RequestFrame()
    {
        if (frameRequested || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        frameRequested = true;
        topLevel.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan frameTime)
    {
        frameRequested = false;
        if (!running)
        {
            return;
        }

        if (IsEffectivelyVisible)
        {
            InvalidateVisual();
        }

        RequestFrame();
    }
}
