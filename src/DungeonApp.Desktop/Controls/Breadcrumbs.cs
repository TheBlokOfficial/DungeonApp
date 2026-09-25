using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Where the shown thing sits: its path as one line of segments, the last one being the thing itself
/// (the current segment). Segments are text, not links - there is nowhere to navigate yet. The look
/// (colours, size, the separator) belongs to the frame's control theme (Themes/DungeonControls.axaml);
/// this class only lays the segments out: when the line does not fit, the current segment keeps its
/// width and the earlier ones (the trail) shorten with an ellipsis; only once the trail is gone does
/// the current segment shorten too (<see cref="BreadcrumbsPanel"/>).
/// </summary>
public sealed class Breadcrumbs : TemplatedControl
{
    public static readonly StyledProperty<IReadOnlyList<string>?> SegmentsProperty =
        AvaloniaProperty.Register<Breadcrumbs, IReadOnlyList<string>?>(nameof(Segments));

    /// <summary>Colour of the current (last) segment; the trail uses <see cref="TemplatedControl.Foreground"/>.</summary>
    public static readonly StyledProperty<IBrush?> CurrentForegroundProperty =
        AvaloniaProperty.Register<Breadcrumbs, IBrush?>(nameof(CurrentForeground));

    public static readonly StyledProperty<DrawingImage?> SeparatorIconProperty =
        AvaloniaProperty.Register<Breadcrumbs, DrawingImage?>(nameof(SeparatorIcon));

    public static readonly StyledProperty<IBrush?> SeparatorForegroundProperty =
        AvaloniaProperty.Register<Breadcrumbs, IBrush?>(nameof(SeparatorForeground));

    public static readonly StyledProperty<double> SeparatorSizeProperty =
        AvaloniaProperty.Register<Breadcrumbs, double>(nameof(SeparatorSize), 8);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<Breadcrumbs, double>(nameof(Spacing));

    private BreadcrumbsPanel? _panel;

    public IReadOnlyList<string>? Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public IBrush? CurrentForeground
    {
        get => GetValue(CurrentForegroundProperty);
        set => SetValue(CurrentForegroundProperty, value);
    }

    public DrawingImage? SeparatorIcon
    {
        get => GetValue(SeparatorIconProperty);
        set => SetValue(SeparatorIconProperty, value);
    }

    public IBrush? SeparatorForeground
    {
        get => GetValue(SeparatorForegroundProperty);
        set => SetValue(SeparatorForegroundProperty, value);
    }

    public double SeparatorSize
    {
        get => GetValue(SeparatorSizeProperty);
        set => SetValue(SeparatorSizeProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<BreadcrumbsPanel>("PART_Panel");
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SegmentsProperty)
        {
            Rebuild();
        }
    }

    // Segment and separator controls are built here, not in XAML: their count comes from the value.
    // Every look property is bound to this control's styled properties, which the theme sets.
    private void Rebuild()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Children.Clear();
        var segments = Segments ?? Array.Empty<string>();
        for (var index = 0; index < segments.Count; index++)
        {
            if (index > 0)
            {
                var separator = new ForegroundIcon
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    IsHitTestVisible = false,
                    [!ForegroundIcon.SourceProperty] = this[!SeparatorIconProperty],
                    [!ForegroundIcon.ForegroundProperty] = this[!SeparatorForegroundProperty],
                    [!WidthProperty] = this[!SeparatorSizeProperty],
                    [!HeightProperty] = this[!SeparatorSizeProperty],
                };
                _panel.Children.Add(separator);
            }

            var isCurrent = index == segments.Count - 1;
            var text = new TextBlock
            {
                Text = segments[index],
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                ClipToBounds = true,
                [!TextBlock.ForegroundProperty] = isCurrent ? this[!CurrentForegroundProperty] : this[!ForegroundProperty],
            };
            _panel.Children.Add(text);
        }
    }
}

/// <summary>
/// One line of breadcrumb children: segment, separator, segment, ... The last child is the current
/// segment. Space goes first to the current segment (up to its full width), then to the separators,
/// and what is left is shared by the trail segments so that a short segment keeps its full width and
/// the longer ones get an equal share of the rest (and shorten with an ellipsis). When not even the
/// separators fit next to the current segment, the trail is not shown.
/// </summary>
public sealed class BreadcrumbsPanel : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<BreadcrumbsPanel, double>(nameof(Spacing));

    private double[] _widths = [];

    static BreadcrumbsPanel()
    {
        AffectsMeasure<BreadcrumbsPanel>(SpacingProperty);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children;
        var count = children.Count;
        _widths = new double[count];
        if (count == 0)
        {
            return default;
        }

        var height = 0.0;
        var desired = new double[count];
        for (var index = 0; index < count; index++)
        {
            children[index].Measure(Size.Infinity);
            desired[index] = children[index].DesiredSize.Width;
            height = Math.Max(height, children[index].DesiredSize.Height);
        }

        var available = availableSize.Width;
        var current = count - 1;
        _widths[current] = Math.Min(desired[current], available);

        // Separators sit at odd indices, trail segments at even indices below the current one.
        var fixedTrail = 0.0;
        var segments = new List<int>();
        for (var index = 0; index < current; index++)
        {
            fixedTrail += Spacing;
            if (index % 2 == 1)
            {
                fixedTrail += desired[index];
            }
            else
            {
                segments.Add(index);
            }
        }

        var remaining = available - _widths[current] - fixedTrail;
        if (current > 0 && remaining > 0)
        {
            for (var index = 1; index < current; index += 2)
            {
                _widths[index] = desired[index];
            }

            // Water-filling: segments narrower than the fair share keep their width.
            var open = new List<int>(segments);
            var progress = true;
            while (open.Count > 0 && progress)
            {
                progress = false;
                var share = remaining / open.Count;
                foreach (var index in open.ToArray())
                {
                    if (desired[index] <= share)
                    {
                        _widths[index] = desired[index];
                        remaining -= desired[index];
                        open.Remove(index);
                        progress = true;
                    }
                }
            }

            foreach (var index in open)
            {
                _widths[index] = remaining / open.Count;
            }
        }

        var total = 0.0;
        var shown = 0;
        for (var index = 0; index < count; index++)
        {
            if (_widths[index] > 0 || index == current)
            {
                children[index].Measure(new Size(_widths[index], availableSize.Height));
                total += _widths[index];
                shown++;
            }
        }

        total += Math.Max(0, shown - 1) * Spacing;
        return new Size(double.IsInfinity(available) ? total : Math.Min(total, available), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        for (var index = 0; index < Children.Count; index++)
        {
            var width = index < _widths.Length ? _widths[index] : 0;
            var isCurrent = index == Children.Count - 1;
            if (width <= 0 && !isCurrent)
            {
                Children[index].Arrange(default);
                continue;
            }

            Children[index].Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width + Spacing;
        }

        return finalSize;
    }
}
