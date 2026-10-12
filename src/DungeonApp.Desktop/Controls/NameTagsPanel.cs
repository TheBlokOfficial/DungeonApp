using System;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Two children in a row: the name (first) on the left and the tags (second) at the right edge.
/// The name has priority and keeps its natural width, but never takes the room reserved for the tags;
/// the tags take the rest and trim themselves. The two never overlap, however long either is.
/// </summary>
public sealed class NameTagsPanel : Panel
{
    /// <summary>Gap kept between the name and the tags.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<NameTagsPanel, double>(nameof(Spacing));

    /// <summary>Share of the row always left to the tags (or all of them, when they are shorter).</summary>
    private const double MinTagsShare = 0.2;

    /// <summary>Gap between the name and the tags.</summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    static NameTagsPanel()
    {
        AffectsMeasure<NameTagsPanel>(SpacingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2)
        {
            return base.MeasureOverride(availableSize);
        }

        var (nameChild, tagsChild) = (Children[0], Children[1]);
        nameChild.Measure(Size.Infinity);
        tagsChild.Measure(Size.Infinity);
        var (nameWidth, tagsWidth) = Split(
            availableSize.Width, nameChild.DesiredSize.Width, tagsChild.DesiredSize.Width);
        nameChild.Measure(new Size(nameWidth, availableSize.Height));
        tagsChild.Measure(new Size(tagsWidth, availableSize.Height));
        var width = nameWidth + tagsWidth + (tagsWidth > 0 ? Spacing : 0);
        return new Size(
            double.IsInfinity(availableSize.Width) ? width : availableSize.Width,
            Math.Max(nameChild.DesiredSize.Height, tagsChild.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2)
        {
            return base.ArrangeOverride(finalSize);
        }

        var (nameChild, tagsChild) = (Children[0], Children[1]);
        var (nameWidth, tagsWidth) = Split(
            finalSize.Width, nameChild.DesiredSize.Width, tagsChild.DesiredSize.Width);
        nameChild.Arrange(new Rect(0, 0, nameWidth, finalSize.Height));
        tagsChild.Arrange(new Rect(finalSize.Width - tagsWidth, 0, tagsWidth, finalSize.Height));
        return finalSize;
    }

    private (double Name, double Tags) Split(double available, double nameNatural, double tagsNatural)
    {
        if (double.IsInfinity(available))
        {
            return (nameNatural, tagsNatural);
        }

        if (tagsNatural <= 0)
        {
            return (Math.Min(nameNatural, available), 0);
        }

        var tagsMin = Math.Min(tagsNatural, available * MinTagsShare);
        var name = Math.Max(0, Math.Min(nameNatural, available - Spacing - tagsMin));
        var tags = Math.Max(0, Math.Min(tagsNatural, available - Spacing - name));
        return (name, tags);
    }
}
