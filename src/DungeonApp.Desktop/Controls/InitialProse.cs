using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A paragraph of a card's text opened by an icon the way a manuscript opens with an initial: the icon
/// is a square two body lines tall in the top left corner, the first two lines start after it (a
/// DungeonSpacingSm gap), every further line starts at the left edge. The text is written in
/// <see cref="EmphasisMarkup"/> and shown as <c>body secondary</c> selectable text, like a prose section.
/// <para>
/// A text block wraps in one rectangle only, so the paragraph is two blocks: the lines that fit beside
/// the icon and the rest under it at full width. Where to cut is read from the real layout of the text
/// at the narrower width - the end of its second line - so a word is never broken and a hard line
/// break counts as a line end; the whitespace at the cut is dropped. Both blocks share one line
/// height and the second starts where the second line ends, so the rhythm runs on unbroken. The cut is
/// found again in every measure whose width or text differs from the last one - no timers.
/// </para>
/// <para>Selection stays within one block; selecting across the cut is not supported.</para>
/// </summary>
public sealed class InitialProse : Control
{
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<InitialProse, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<InitialProse, string?>(nameof(Text));

    private static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<InitialProse, double>("Gap");

    private readonly ForegroundIcon _icon = new();
    private readonly SelectableTextBlock _lead = NewBlock();
    private readonly SelectableTextBlock _rest = NewBlock();

    private IReadOnlyList<EmphasisRun> _runs = [];
    private bool _runsChanged = true;
    private double _cutWidth = double.NaN;

    static InitialProse()
    {
        AffectsMeasure<InitialProse>(GapProperty);
    }

    public InitialProse()
    {
        _icon.Bind(ForegroundIcon.ForegroundProperty, _icon.GetResourceObservable("DungeonTextMutedBrush"));
        Bind(GapProperty, this.GetResourceObservable("DungeonSpacingSm"));
        _rest.IsVisible = false;

        VisualChildren.Add(_icon);
        VisualChildren.Add(_lead);
        VisualChildren.Add(_rest);
        // The icon is a logical child too: resources (its muted brush) are found up the logical tree.
        LogicalChildren.Add(_icon);
        LogicalChildren.Add(_lead);
        LogicalChildren.Add(_rest);
    }

    /// <summary>The initial: an outline icon from the icon set, drawn in the muted text color.</summary>
    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The paragraph; may carry <see cref="EmphasisMarkup"/>.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private double Gap => GetValue(GapProperty);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty)
        {
            _icon.Source = Icon;
        }
        else if (change.Property == TextProperty)
        {
            _runs = EmphasisMarkup.Parse(Text ?? string.Empty);
            _runsChanged = true;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var leadWidth = Math.Max(0, width - IconSide() - Gap);

        if (_runsChanged || !leadWidth.Equals(_cutWidth))
        {
            Cut(leadWidth);
        }

        _lead.Measure(new Size(leadWidth, double.PositiveInfinity));
        _rest.Measure(new Size(width, double.PositiveInfinity));

        var side = IconSide();
        _icon.Measure(new Size(side, side));

        var height = Math.Max(side, _lead.DesiredSize.Height) + (_rest.IsVisible ? _rest.DesiredSize.Height : 0);
        var desiredWidth = double.IsInfinity(width)
            ? Math.Max(side + Gap + _lead.DesiredSize.Width, _rest.IsVisible ? _rest.DesiredSize.Width : 0)
            : width;
        return new Size(desiredWidth, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var side = IconSide();
        _icon.Arrange(new Rect(0, 0, side, side));
        _lead.Arrange(new Rect(side + Gap, 0, Math.Max(0, finalSize.Width - side - Gap), _lead.DesiredSize.Height));
        _rest.Arrange(new Rect(0, Math.Max(side, _lead.DesiredSize.Height), finalSize.Width, _rest.DesiredSize.Height));
        return finalSize;
    }

    private static SelectableTextBlock NewBlock() => new()
    {
        TextWrapping = TextWrapping.Wrap,
        Classes = { "body", "secondary" },
    };

    /// <summary>
    /// Two lines of the text's own role: the role's line height when it sets one, else the height of
    /// a laid-out line - never a number of this control's own.
    /// </summary>
    private double IconSide()
    {
        if (!double.IsNaN(_lead.LineHeight) && _lead.LineHeight > 0)
        {
            return 2 * _lead.LineHeight;
        }

        return _lead.TextLayout.TextLines.Count > 0 ? 2 * _lead.TextLayout.TextLines[0].Height : 0;
    }

    /// <summary>
    /// Lays the whole text out beside the icon and splits it after the second line of that layout:
    /// the head stays beside the icon, the tail goes under it.
    /// </summary>
    private void Cut(double leadWidth)
    {
        _runsChanged = false;
        _cutWidth = leadWidth;

        _lead.Inlines = EmphasisMarkup.ToInlines(_runs);
        _lead.Measure(new Size(leadWidth, double.PositiveInfinity));

        var lines = _lead.TextLayout.TextLines;
        var cut = lines.Count > 2 ? lines[1].FirstTextSourceIndex + lines[1].Length : int.MaxValue;
        var tail = TrimStart(Slice(_runs, cut, int.MaxValue));

        if (tail.Count == 0)
        {
            _rest.Inlines = null;
            _rest.IsVisible = false;
            return;
        }

        _lead.Inlines = EmphasisMarkup.ToInlines(TrimEnd(Slice(_runs, 0, cut)));
        _rest.Inlines = EmphasisMarkup.ToInlines(tail);
        _rest.IsVisible = true;
    }

    /// <summary>The runs' characters in <c>[start, end)</c>, run boundaries and emphasis kept.</summary>
    private static List<EmphasisRun> Slice(IReadOnlyList<EmphasisRun> runs, int start, int end)
    {
        var slice = new List<EmphasisRun>();
        var position = 0;

        foreach (var run in runs)
        {
            var from = Math.Max(start, position);
            var to = Math.Min(end, position + run.Text.Length);
            if (to > from)
            {
                slice.Add(run with { Text = run.Text[(from - position)..(to - position)] });
            }

            position += run.Text.Length;
        }

        return slice;
    }

    private static List<EmphasisRun> TrimStart(List<EmphasisRun> runs)
    {
        while (runs.Count > 0)
        {
            var trimmed = runs[0].Text.TrimStart();
            if (trimmed.Length > 0)
            {
                runs[0] = runs[0] with { Text = trimmed };
                break;
            }

            runs.RemoveAt(0);
        }

        return runs;
    }

    private static List<EmphasisRun> TrimEnd(List<EmphasisRun> runs)
    {
        while (runs.Count > 0)
        {
            var trimmed = runs[^1].Text.TrimEnd();
            if (trimmed.Length > 0)
            {
                runs[^1] = runs[^1] with { Text = trimmed };
                break;
            }

            runs.RemoveAt(runs.Count - 1);
        }

        return runs;
    }
}
