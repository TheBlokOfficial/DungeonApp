using System;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A row of tags that never wraps: the tags that fit stand at their own width, the first one that
/// does not is given what is left of the line (its text trims with an ellipsis and opens whole under
/// the pointer, as the tag's own text block does), and the tags after it are not shown. A remainder
/// too narrow to read as a tag (<see cref="MinimumTagWidth"/>) shows nothing at all, so the row never
/// ends in a sliver. The row is always one tag tall, so a header column that holds it has a height
/// that does not depend on how many tags there are.
/// </summary>
public sealed class OneLineTagPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<OneLineTagPanel, double>(nameof(ItemSpacing));

    /// <summary>The narrowest remainder worth showing as a trimmed tag.</summary>
    public const double MinimumTagWidth = 48;

    private int _shownCount;

    static OneLineTagPanel()
    {
        AffectsMeasure<OneLineTagPanel>(ItemSpacingProperty);
        ClipToBoundsProperty.OverrideDefaultValue<OneLineTagPanel>(true);
    }

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var x = 0.0;
        var height = 0.0;
        var ended = false;
        _shownCount = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);

            if (ended)
            {
                // Past the tag that ended the row: measured so it can be arranged, not shown.
                continue;
            }

            var start = i == 0 ? 0 : x + ItemSpacing;
            var room = availableSize.Width - start;

            if (child.DesiredSize.Width <= room)
            {
                x = start + child.DesiredSize.Width;
                _shownCount = i + 1;
                continue;
            }

            if (room >= MinimumTagWidth)
            {
                child.Measure(new Size(room, availableSize.Height));
                x = start + child.DesiredSize.Width;
                _shownCount = i + 1;
            }

            ended = true;
        }

        return new Size(x, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (i >= _shownCount)
            {
                // Out of the panel's own box, which clips: a hidden tag draws nothing.
                child.Arrange(new Rect(new Point(finalSize.Width, 0), child.DesiredSize));
                continue;
            }

            var start = x == 0 ? 0 : x + ItemSpacing;
            child.Arrange(new Rect(start, 0, child.DesiredSize.Width, finalSize.Height));
            x = start + child.DesiredSize.Width;
        }

        return finalSize;
    }
}
