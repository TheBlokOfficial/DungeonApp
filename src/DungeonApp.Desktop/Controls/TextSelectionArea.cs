using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Selecting text the way a browser does: a drag that starts beside a selectable text - in the empty
/// space around it, left of a line, between two blocks - and runs over it selects it, not only a drag
/// that starts on a letter. The drag selects within one text, the one nearest to where it started;
/// selecting across several texts at once is a different and larger thing.
/// <para>
/// Wraps the content whose <see cref="SelectableTextBlock"/>s it serves. A press something else
/// handles itself - a button, a section header, the text itself - is left to it. Changes nothing but
/// the view's selection and focus.
/// </para>
/// </summary>
public sealed class TextSelectionArea : Border
{
    private SelectableTextBlock? _target;
    private int _anchor;

    public TextSelectionArea()
    {
        // Empty space must take the press too, or a drag could only start on something drawn.
        Background = Brushes.Transparent;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Handled || e.ClickCount > 1 || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (Nearest(e.GetPosition(this)) is not { } target)
        {
            return;
        }

        _target = target;
        _anchor = HitTest(target, e.GetPosition(target));

        target.Focus(NavigationMethod.Pointer, e.KeyModifiers);
        target.SelectionStart = _anchor;
        target.SelectionEnd = _anchor;

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_target is null || e.Pointer.Captured != this)
        {
            return;
        }

        _target.SelectionStart = _anchor;
        _target.SelectionEnd = HitTest(_target, e.GetPosition(_target));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_target is not null && e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }

        _target = null;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _target = null;
    }

    /// <summary>The visible text with something to select whose box lies closest to <paramref name="point"/>.</summary>
    private SelectableTextBlock? Nearest(Point point) =>
        this.GetVisualDescendants()
            .OfType<SelectableTextBlock>()
            .Where(text => text.IsEffectivelyVisible && text.IsEffectivelyEnabled && HasText(text))
            .Select(text => (Text: text, Distance: DistanceTo(text, point)))
            .Where(candidate => candidate.Distance is not null)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Text)
            .FirstOrDefault();

    /// <summary>
    /// A text written as runs (<see cref="EmphasisMarkup"/>, a value with its muted note) carries
    /// its content in its inlines, not in <see cref="TextBlock.Text"/>.
    /// </summary>
    private static bool HasText(SelectableTextBlock text) =>
        !string.IsNullOrEmpty(text.Text) || !string.IsNullOrEmpty(text.Inlines?.Text);

    private double? DistanceTo(SelectableTextBlock text, Point point)
    {
        if (text.TranslatePoint(default, this) is not { } origin)
        {
            return null;
        }

        var box = new Rect(origin, text.Bounds.Size);
        var dx = Math.Max(0, Math.Max(box.Left - point.X, point.X - box.Right));
        var dy = Math.Max(0, Math.Max(box.Top - point.Y, point.Y - box.Bottom));
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// The character position nearest to <paramref name="point"/>, in the text's own coordinates. A
    /// point outside the text clamps to it: left of a line is the line's start, right of it the
    /// line's end, above the text its first line, below it the last.
    /// </summary>
    private static int HitTest(SelectableTextBlock text, Point point)
    {
        var padding = text.Padding;
        return text.TextLayout.HitTestPoint(new Point(point.X - padding.Left, point.Y - padding.Top)).TextPosition;
    }
}
