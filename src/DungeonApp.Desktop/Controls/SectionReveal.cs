using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Shows and hides the content of a foldable section (an Expander's content, a card section under its
/// heading) by unrolling it: the space the content takes grows from nothing to its full height while
/// the content fades in, and shrinks back while it fades out, so whatever stands below moves smoothly
/// instead of jumping. The content is drawn at its full height and clipped to the space it has so far.
/// <para>
/// Opening eases out, closing eases in and out. The time grows with the content's height, from
/// DungeonSectionRevealShortDuration to DungeonSectionRevealLongDuration, so a long text does not
/// unroll faster than a short one. Flipping <see cref="IsOpen"/> mid-way turns the motion back from where
/// it is. Opening content takes the pointer from the first frame; closing content takes it no more, and
/// once closed it is not visible at all - no space, no hit testing, no focus.
/// </para>
/// <para>
/// The state changes at once and the motion only catches up. Without motion: before the first layout
/// (a section built open or closed shows so at once), detached from a window, and with animations off
/// in Windows (<see cref="Themes.SystemMotion.IsReduced"/>). Frames come from the window
/// (<see cref="TopLevel.RequestAnimationFrame"/>); the motion changes the view only, never the campaign.
/// </para>
/// </summary>
public sealed class SectionReveal : Decorator
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<SectionReveal, bool>(nameof(IsOpen), defaultValue: true);

    /// <summary>Content height (px) unrolled in the short time; shorter content takes the same.</summary>
    private const double ShortHeight = 60;

    /// <summary>Content height (px) unrolled in the long time; taller content takes the same.</summary>
    private const double LongHeight = 600;

    private static readonly IEasing OpenEasing = new CubicEaseOut();
    private static readonly IEasing CloseEasing = new CubicEaseInOut();

    private readonly Stopwatch _clock = new();
    private Size _lastAvailable = new(double.PositiveInfinity, double.PositiveInfinity);
    private double _progress = 1d;
    private double _from;
    private double _to;
    private TimeSpan _duration;
    private bool _running;
    private bool _frameRequested;
    private bool _laidOut;

    /// <summary>True while the content is shown (or unrolling to be).</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Share of the content's height the section takes now: 0 closed, 1 open.</summary>
    public double Progress => _progress;

    /// <summary>Whether the content is unrolling or rolling up now.</summary>
    public bool IsMoving => _running;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty)
        {
            if (_laidOut && !Themes.SystemMotion.IsReduced && TopLevel.GetTopLevel(this) is not null && Child is not null)
            {
                Start(IsOpen);
            }
            else
            {
                Settle(IsOpen);
            }
        }
        else if (change.Property == ChildProperty)
        {
            ApplyToChild();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_running)
        {
            Settle(IsOpen);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _lastAvailable = availableSize;
        var desired = base.MeasureOverride(availableSize);
        return _progress >= 1d ? desired : new Size(desired.Width, desired.Height * _progress);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _laidOut = true;
        if (!_running || Child is not { } child)
        {
            return base.ArrangeOverride(finalSize);
        }

        // Laid out at full height from the top and clipped: the content unrolls downwards.
        var padding = Padding;
        var height = Math.Max(finalSize.Height, child.DesiredSize.Height + padding.Top + padding.Bottom);
        child.Arrange(new Rect(new Size(finalSize.Width, height)).Deflate(padding));
        return finalSize;
    }

    private void Start(bool open)
    {
        _to = open ? 1d : 0d;
        _from = _progress;
        if (Math.Abs(_to - _from) < 0.001)
        {
            Settle(open);
            return;
        }

        if (open && Child is { IsVisible: false } hidden)
        {
            // Visible before it is measured: its height sets the time.
            hidden.IsVisible = true;
            hidden.Measure(_lastAvailable);
        }

        _duration = DurationFor(Child?.DesiredSize.Height ?? 0d) * Math.Abs(_to - _from);
        _running = true;
        ClipToBounds = true;
        _clock.Restart();
        ApplyToChild();
        InvalidateMeasure();
        RequestFrame();
    }

    private void Settle(bool open)
    {
        _running = false;
        _clock.Reset();
        _progress = open ? 1d : 0d;
        ClipToBounds = false;
        ApplyToChild();
        InvalidateMeasure();
    }

    private void RequestFrame()
    {
        if (_frameRequested || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        _frameRequested = true;
        topLevel.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan frameTime)
    {
        _frameRequested = false;
        if (!_running)
        {
            return;
        }

        var t = _duration > TimeSpan.Zero ? _clock.Elapsed / _duration : 1d;
        if (t >= 1d)
        {
            Settle(_to >= 1d);
            return;
        }

        var easing = _to > _from ? OpenEasing : CloseEasing;
        _progress = _from + (_to - _from) * easing.Ease(t);
        ApplyToChild();
        InvalidateMeasure();
        RequestFrame();
    }

    private void ApplyToChild()
    {
        if (Child is not { } child)
        {
            return;
        }

        child.IsVisible = IsOpen || _running;
        child.IsHitTestVisible = IsOpen;
        child.Opacity = _progress;
    }

    /// <summary>
    /// Time to unroll content of <paramref name="height"/> whole way: the short time up to
    /// <see cref="ShortHeight"/>, the long one from <see cref="LongHeight"/>, in proportion between.
    /// </summary>
    private TimeSpan DurationFor(double height)
    {
        var shortTime = Resource("DungeonSectionRevealShortDuration", TimeSpan.FromMilliseconds(180));
        var longTime = Resource("DungeonSectionRevealLongDuration", TimeSpan.FromMilliseconds(320));
        var share = Math.Clamp((height - ShortHeight) / (LongHeight - ShortHeight), 0d, 1d);
        return shortTime + (longTime - shortTime) * share;
    }

    private TimeSpan Resource(string key, TimeSpan fallback) =>
        this.TryFindResource(key, out var value) && value is TimeSpan span ? span : fallback;
}
