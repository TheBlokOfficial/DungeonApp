using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Selectable text that, when trimmed, shows itself whole in place while the pointer is over it -
/// like a trimmed name in the Windows Explorer tree: the full line opens exactly over the trimmed
/// one, in the same type, unwrapped, on the card surface (DungeonBackstageCardBrush), and may run
/// past the panel's edge, covering what stands beside the text for as long as it is open. Nothing
/// opens when the text is not trimmed (wrapping text never is).
/// <para>
/// It is a <see cref="SelectableTextBlock"/> to styles and themes (<see cref="StyleKeyOverride"/>), so
/// the typography classes and the frame's theme apply to it unchanged; trimming and wrapping are set
/// by the host as on any text block (<see cref="TightCharacterEllipsis"/> for a name). The full line
/// lives in a popup on the window's overlay layer that takes no pointer input: the pointer stays over
/// the text underneath, so the popup cannot make it leave and come back (flicker), and selecting still
/// works. Leaving the text fades it out; a press, the wheel, a new text or a new size close it at once.
/// </para>
/// <para>
/// The popup sits on the overlay, outside the text's ancestors, so nothing of the text's look is
/// inherited there: every property that shapes or colors the glyphs is bound to the text's own value,
/// however the text got it (style, theme or local value).
/// </para>
/// <para>
/// The popup draws only what the full line adds - from where the trimmed text stops (before the
/// ellipsis) to its end. The letters both share stay the text's own, drawn once, so they cannot
/// change color or weight at any moment. Opacity is applied to each primitive on its own, not to the
/// popup as one picture: a second copy of those letters fading in over the first would dim them
/// mid-fade. What the popup adds is one opaque rectangle and the rest of the line over it, faded
/// together: the rectangle hides the ellipsis (and whatever stands beside the text) by exactly its
/// opacity while the rest of the line comes in. With animations off in Windows
/// (<see cref="Themes.SystemMotion.IsReduced"/>) it opens and closes at once.
/// </para>
/// </summary>
public class RevealingTextBlock : SelectableTextBlock, ICustomHitTest
{
    private const string SurfaceBrushKey = "DungeonBackstageCardBrush";

    /// <summary>Far enough for any line: the clip of the added part has no right or vertical bound.</summary>
    private const double Unbounded = 100_000;

    private readonly TextBlock _fullText;
    private readonly Border _surface;
    private readonly FadingCover _cover;
    private readonly Popup _popup;
    private readonly DispatcherTimer _fadeEnd;
    private bool _revealed;

    public RevealingTextBlock()
    {
        _fullText = new TextBlock { TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.None };
        foreach (var property in new AvaloniaProperty[]
                 {
                     TextProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty,
                     FontStretchProperty, FontFeaturesProperty, LetterSpacingProperty, LineHeightProperty,
                     LineSpacingProperty, BaselineOffsetProperty, ForegroundProperty, PaddingProperty,
                     TextDecorationsProperty, FlowDirectionProperty,
                 })
        {
            _fullText.Bind(property, this.GetObservable(property));
        }

        _surface = new Border { IsHitTestVisible = false };
        _surface.Bind(Border.BackgroundProperty, _surface.GetResourceObservable(SurfaceBrushKey));
        _cover = new FadingCover(_surface, _fullText) { IsHitTestVisible = false };
        _popup = new Popup
        {
            Child = _cover,
            PlacementTarget = this,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.TopLeft,
            PlacementGravity = PopupGravity.BottomRight,
            ShouldUseOverlayLayer = true,
            IsLightDismissEnabled = false,
            IsHitTestVisible = false,
            Focusable = false,
        };
        _fadeEnd = new DispatcherTimer();
        _fadeEnd.Tick += OnFadeEnd;

        // A logical child, so the popup's content resolves the window's styles and resources.
        LogicalChildren.Add(_popup);
    }

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    /// <summary>Whether the text is drawn shortened: some line of it ends in the trimming mark.</summary>
    public bool IsTrimmed => TextLayout.TextLines.Any(line => line.HasCollapsed);

    /// <summary>Whether the full text stands over the trimmed one now (or is fading in to do so).</summary>
    public bool IsRevealed => _revealed;

    /// <summary>
    /// The whole box is under the pointer, not only the glyphs: the gaps between letters and the
    /// line's empty space must not close and reopen the full line as the pointer crosses them.
    /// </summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Reveal();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        FadeOut();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // Selecting works on the trimmed text underneath, so the cover steps aside for it.
        Conceal();
        base.OnPointerPressed(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // The overlay does not scroll with the text: fading, it would be seen left behind.
        Conceal();
        base.OnPointerWheelChanged(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Conceal();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == BoundsProperty)
        {
            Conceal();
        }
    }

    /// <summary>Opens the full line over the text when the text is trimmed; otherwise does nothing.</summary>
    public void Reveal()
    {
        if (!IsTrimmed || string.IsNullOrEmpty(Text) || TopLevel.GetTopLevel(this) is null)
        {
            return;
        }

        var cut = CutPoint();
        _surface.Margin = new Thickness(cut, 0, 0, 0);
        _fullText.Clip = new RectangleGeometry(new Rect(cut, -Unbounded, Unbounded, 2 * Unbounded));

        _revealed = true;
        _fadeEnd.Stop();
        var duration = FadeDuration();

        if (!_popup.IsOpen)
        {
            // Without transitions, so the fade starts from nothing rather than from where the last one
            // was cut short.
            _cover.Transitions = null;
            _cover.Fade = duration > TimeSpan.Zero ? 0d : 1d;
            _popup.IsOpen = true;
        }

        if (duration > TimeSpan.Zero)
        {
            _cover.Transitions ??= [new DoubleTransition { Property = FadingCover.FadeProperty, Duration = duration, Easing = new CubicEaseOut() }];
        }

        _cover.FadeTo(1d);
    }

    /// <summary>Closes the full line at once, with no fade.</summary>
    public void Conceal()
    {
        _revealed = false;
        _fadeEnd.Stop();
        _popup.IsOpen = false;
    }

    private void FadeOut()
    {
        var duration = FadeDuration();
        if (!_popup.IsOpen || duration <= TimeSpan.Zero)
        {
            Conceal();
            return;
        }

        _revealed = false;
        _cover.FadeTo(0d);
        _fadeEnd.Stop();
        _fadeEnd.Interval = duration;
        _fadeEnd.Start();
    }

    private void OnFadeEnd(object? sender, EventArgs e)
    {
        _fadeEnd.Stop();
        if (!_revealed)
        {
            _popup.IsOpen = false;
        }
    }

    /// <summary>
    /// Where the letters the trimmed text shares with the full line end, from the text's left edge:
    /// the trimmed line's runs up to its ellipsis, the last run. Names are laid out left to right.
    /// Snapped down to a whole device pixel, so layout rounding leaves the surface exactly where the
    /// full line's clip starts and the surface covers the whole ellipsis; the sliver of the last kept
    /// letter this takes in is drawn again by the full line, in the same place.
    /// </summary>
    private double CutPoint()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return Math.Floor(UnsnappedCutPoint() * scale) / scale;
    }

    private double UnsnappedCutPoint()
    {
        var line = TextLayout.TextLines.First(textLine => textLine.HasCollapsed);
        var cut = line.Start;
        var runs = line.TextRuns;
        for (var index = 0; index < runs.Count - 1; index++)
        {
            if (runs[index] is DrawableTextRun drawable)
            {
                cut += drawable.Size.Width;
            }
        }

        return Padding.Left + cut;
    }

    /// <summary>DungeonPopupOpenDuration, the frame's time for a popup to appear; none with animations off.</summary>
    private TimeSpan FadeDuration()
    {
        if (Themes.SystemMotion.IsReduced)
        {
            return TimeSpan.Zero;
        }

        return this.TryFindResource("DungeonPopupOpenDuration", out var value) && value is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(120);
    }

    /// <summary>
    /// The popup's content: the opaque surface from the cut point on, and the full line over it
    /// clipped to the same part. One fade drives both, each as its own primitive.
    /// </summary>
    private sealed class FadingCover : Panel
    {
        public static readonly StyledProperty<double> FadeProperty =
            AvaloniaProperty.Register<FadingCover, double>(nameof(Fade), 1d);

        public FadingCover(Border surface, TextBlock fullText)
        {
            Children.Add(surface);
            Children.Add(fullText);
        }

        public double Fade
        {
            get => GetValue(FadeProperty);
            set => SetValue(FadeProperty, value);
        }

        /// <summary>
        /// Starts the fade towards <paramref name="target"/> (through the transition, when there is one).
        /// A transition advances only on frames, and a window with nothing to redraw asks for none: the
        /// fade would stand still at its start and the popup never close. Asking for a frame here starts
        /// it; each step of it then asks for the next.
        /// </summary>
        public void FadeTo(double target)
        {
            Fade = target;
            InvalidateVisual();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == FadeProperty)
            {
                foreach (var child in Children)
                {
                    child.Opacity = Fade;
                }
            }
        }
    }
}
