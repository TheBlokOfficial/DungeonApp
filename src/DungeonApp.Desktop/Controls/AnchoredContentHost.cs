using System;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Gives a static backstage page a bounded reading area without centring it in an ultrawide shell.
/// The host itself fills its parent, but its one child is always arranged from the top-left corner
/// at the smaller of the available size and the configured content limits.
/// </summary>
public sealed class AnchoredContentHost : Decorator
{
    public static readonly StyledProperty<double> MaxContentWidthProperty =
        AvaloniaProperty.Register<AnchoredContentHost, double>(nameof(MaxContentWidth), 1104d);

    public static readonly StyledProperty<double> MaxContentHeightProperty =
        AvaloniaProperty.Register<AnchoredContentHost, double>(nameof(MaxContentHeight), 782d);

    public double MaxContentWidth
    {
        get => GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    public double MaxContentHeight
    {
        get => GetValue(MaxContentHeightProperty);
        set => SetValue(MaxContentHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is null)
        {
            return availableSize;
        }

        Child.Measure(Constrain(availableSize));

        // The host must occupy the shell's whole viewport; only its child is bounded.
        return new Size(
            double.IsPositiveInfinity(availableSize.Width) ? Child.DesiredSize.Width : availableSize.Width,
            double.IsPositiveInfinity(availableSize.Height) ? Child.DesiredSize.Height : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(Constrain(finalSize)));
        return finalSize;
    }

    private Size Constrain(Size availableSize) => new(
        Limit(availableSize.Width, MaxContentWidth),
        Limit(availableSize.Height, MaxContentHeight));

    private static double Limit(double available, double maximum) =>
        double.IsPositiveInfinity(available) ? maximum : Math.Min(available, maximum);
}
