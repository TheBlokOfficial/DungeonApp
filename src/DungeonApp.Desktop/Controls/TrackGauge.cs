using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A tracked value as a flat, square-cornered bar from zero to its maximum: the part that is left
/// and the room above it. It only shows the ratio - it cannot be dragged and does not react to the
/// pointer; the value changes in the change field. A value above the maximum stretches the scale to
/// the value and marks the maximum with a notch, so the surplus reads as more, not as an error.
/// A changed value or maximum glides to the new length (<see cref="DisplayedValue"/>), eased, in code:
/// the first value shown, a hidden or detached gauge and reduced system motion all stand on the target
/// at once. Changes appearance only, never application state.
/// Drawn, not templated - nothing in it is a control; the brushes and the thickness belong to the
/// frame's control theme (Themes/Controls/TrackGauge.axaml).
/// </summary>
public sealed class TrackGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<TrackGauge, double>(nameof(Value));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<TrackGauge, double>(nameof(Maximum));

    public static readonly StyledProperty<IBrush?> RoomBrushProperty =
        AvaloniaProperty.Register<TrackGauge, IBrush?>(nameof(RoomBrush));

    public static readonly StyledProperty<IBrush?> FillBrushProperty =
        AvaloniaProperty.Register<TrackGauge, IBrush?>(nameof(FillBrush));

    public static readonly StyledProperty<IBrush?> OverBrushProperty =
        AvaloniaProperty.Register<TrackGauge, IBrush?>(nameof(OverBrush));

    /// <summary>Gap cut into the bar at the maximum when the value stands above it.</summary>
    public static readonly StyledProperty<double> NotchWidthProperty =
        AvaloniaProperty.Register<TrackGauge, double>(nameof(NotchWidth), 2);

    private static readonly TimeSpan GlideDuration = TimeSpan.FromMilliseconds(280);
    private static readonly Easing GlideEasing = new CubicEaseInOut();

    private readonly Stopwatch glideClock = new();
    private bool drawn;
    private bool gliding;
    private bool frameRequested;
    private double fromValue;
    private double fromMaximum;

    static TrackGauge()
    {
        AffectsRender<TrackGauge>(
            ValueProperty, MaximumProperty, RoomBrushProperty, FillBrushProperty,
            OverBrushProperty, NotchWidthProperty);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IBrush? RoomBrush
    {
        get => GetValue(RoomBrushProperty);
        set => SetValue(RoomBrushProperty, value);
    }

    public IBrush? FillBrush
    {
        get => GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public IBrush? OverBrush
    {
        get => GetValue(OverBrushProperty);
        set => SetValue(OverBrushProperty, value);
    }

    public double NotchWidth
    {
        get => GetValue(NotchWidthProperty);
        set => SetValue(NotchWidthProperty, value);
    }

    /// <summary>The value drawn now; glides to <see cref="Value"/> when that changes.</summary>
    public double DisplayedValue { get; private set; }

    /// <summary>The maximum drawn now; glides with the value, so the notch moves smoothly too.</summary>
    public double DisplayedMaximum { get; private set; }

    // A value the gauge has never drawn is shown as it is: a card appearing, or switching to another
    // entity, does not glide from the previous numbers.
    private bool CanMove => drawn && !Themes.SystemMotion.IsReduced && VisualRoot is not null && IsEffectivelyVisible;

    /// <summary>Draws the current value and maximum at once, without a glide (another entity on the same card).</summary>
    public void StandOnTarget()
    {
        gliding = false;
        glideClock.Stop();
        DisplayedValue = Value;
        DisplayedMaximum = Maximum;
        InvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StandOnTarget();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        drawn = false;
        StandOnTarget();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == MaximumProperty)
        {
            Glide();
        }
        else if (change.Property == IsVisibleProperty && !IsVisible)
        {
            StandOnTarget();
        }
    }

    private void Glide()
    {
        if (!CanMove)
        {
            StandOnTarget();
            return;
        }

        // A change in mid-glide starts the next one from where the bar stands now.
        fromValue = DisplayedValue;
        fromMaximum = DisplayedMaximum;
        gliding = true;
        glideClock.Restart();
        RequestFrame();
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
        if (!gliding)
        {
            return;
        }

        if (!CanMove)
        {
            StandOnTarget();
            return;
        }

        var progress = glideClock.Elapsed.TotalMilliseconds / GlideDuration.TotalMilliseconds;
        if (progress >= 1)
        {
            StandOnTarget();
            return;
        }

        var eased = GlideEasing.Ease(progress);
        DisplayedValue = fromValue + (Value - fromValue) * eased;
        DisplayedMaximum = fromMaximum + (Maximum - fromMaximum) * eased;
        InvalidateVisual();
        RequestFrame();
    }

    public override void Render(DrawingContext context)
    {
        drawn = true;
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var maximum = Math.Max(DisplayedMaximum, 0);
        var value = Math.Max(DisplayedValue, 0);
        var scale = Math.Max(maximum, value);

        Fill(context, RoomBrush, 0, width, height);
        if (scale <= 0)
        {
            return;
        }

        double X(double amount) => Math.Round(Math.Clamp(amount / scale, 0, 1) * width);

        if (value <= maximum)
        {
            Fill(context, FillBrush, 0, X(value), height);
            return;
        }

        var notch = X(maximum);
        Fill(context, FillBrush, 0, notch, height);
        Fill(context, OverBrush, notch + NotchWidth, width, height);
    }

    private static void Fill(DrawingContext context, IBrush? brush, double from, double to, double height)
    {
        if (brush is not null && to > from)
        {
            context.FillRectangle(brush, new Rect(from, 0, to - from, height));
        }
    }
}
