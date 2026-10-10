using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A tracked value as a flat, square-cornered bar from zero to its maximum: the part that is left
/// and the room above it. It only shows the ratio - it cannot be dragged and does not react to the
/// pointer; the value changes in the change field. A value above the maximum stretches the scale to
/// the value and marks the maximum with a notch, so the surplus reads as more, not as an error.
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

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var maximum = Math.Max(Maximum, 0);
        var value = Math.Max(Value, 0);
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
