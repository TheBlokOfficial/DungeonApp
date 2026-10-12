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
/// is a selectable text of its own in a popup on the window's overlay layer, and it takes the pointer
/// over its whole box - the trimmed text's place included: the line can be selected whole, copied
/// with Ctrl+C or its context menu, and a click or a selecting drag leaves it open. It stays open
/// while the pointer is over the text or the line, and fades out only once the pointer has left both:
/// whether it has is asked after the pointer has settled, so crossing from one to the other never
/// closes it (no flicker). The wheel, a new text or a new size close it at once.
/// </para>
/// <para>
/// The popup sits on the overlay, outside the text's ancestors, so nothing of the text's look is
/// inherited there: every property that shapes or colors the glyphs (and the selection) is bound to
/// the text's own value, however the text got it (style, theme or local value).
/// </para>
/// <para>
/// While it fades, the popup draws only what the full line adds - from where the trimmed text stops
/// (before the ellipsis) to its end. The letters both share stay the text's own, drawn once, so they
/// cannot change color or weight at any moment. Opacity is applied to each primitive on its own, not
/// to the popup as one picture: a second copy of those letters fading in over the first would dim
/// them mid-fade. What the popup adds is one opaque rectangle and the rest of the line over it, faded
/// together: the rectangle hides the ellipsis (and whatever stands beside the text) by exactly its
/// opacity while the rest of the line comes in. Only a press on the line, fully opaque by then, makes
/// it draw itself whole on the surface - the same pixels on the card - so a selection can cover the
/// shared letters too; fading out returns to the added part first. With animations off in Windows
/// (<see cref="Themes.SystemMotion.IsReduced"/>) it opens and closes at once.
/// </para>
/// </summary>
public class RevealingTextBlock : SelectableTextBlock, ICustomHitTest
{
    private const string SurfaceBrushKey = "DungeonBackstageCardBrush";

    /// <summary>Far enough for any line: the clip of the added part has no right or vertical bound.</summary>
    private const double Unbounded = 100_000;

    private readonly FullLine _fullLine;
    private readonly Border _surface;
    private readonly FadingCover _cover;
    private readonly Popup _popup;
    private readonly DispatcherTimer _fadeEnd;
    private bool _revealed;
    private bool _pointerCheckPending;
    private FlyoutBase? _awaitedFlyout;

    public RevealingTextBlock()
    {
        _fullLine = new FullLine(this) { TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.None };
        foreach (var property in new AvaloniaProperty[]
                 {
                     TextProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty,
                     FontStretchProperty, FontFeaturesProperty, LetterSpacingProperty, LineHeightProperty,
                     LineSpacingProperty, BaselineOffsetProperty, ForegroundProperty, PaddingProperty,
                     TextDecorationsProperty, FlowDirectionProperty, SelectionBrushProperty,
                     SelectionForegroundBrushProperty,
                 })
        {
            _fullLine.Bind(property, this.GetObservable(property));
        }

        _surface = new Border { IsHitTestVisible = false };
        _surface.Bind(Border.BackgroundProperty, _surface.GetResourceObservable(SurfaceBrushKey));
        _cover = new FadingCover(_surface, _fullLine);
        _popup = new Popup
        {
            Child = _cover,
            PlacementTarget = this,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.TopLeft,
            PlacementGravity = PopupGravity.BottomRight,
            ShouldUseOverlayLayer = true,
            IsLightDismissEnabled = false,
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
        CheckPointerLater();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // The open line takes presses over its whole box; one reaching the text itself (where the
        // line does not reach) selects the trimmed text, so the line steps aside for it.
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

        _revealed = true;
        _fadeEnd.Stop();
        var duration = FadeDuration();

        if (!_popup.IsOpen)
        {
            // At least the text's box, so the line takes the pointer wherever the text did.
            _fullLine.MinWidth = Bounds.Width;
            _fullLine.MinHeight = Bounds.Height;
            ShowAddedPartOnly();

            // Without transitions, so the fade starts from nothing rather than from where the last one
            // was cut short.
            _cover.Transitions = null;
            _cover.Fade = duration > TimeSpan.Zero ? 0d : 1d;
            _popup.IsOpen = true;
        }

        EnsureTransitions(duration);
        _cover.FadeTo(1d);
    }

    /// <summary>Closes the full line at once, with no fade.</summary>
    public void Conceal()
    {
        _revealed = false;
        _fadeEnd.Stop();
        _fullLine.IsHeld = false;
        _fullLine.SelectionStart = 0;
        _fullLine.SelectionEnd = 0;
        _popup.IsOpen = false;
    }

    /// <summary>
    /// A press on the line: it stands fully opened at once and draws itself whole, so the selection
    /// it starts can take in the shared letters too.
    /// </summary>
    private void Settle()
    {
        if (!_popup.IsOpen)
        {
            return;
        }

        _revealed = true;
        _fadeEnd.Stop();
        _cover.Transitions = null;
        _cover.FadeTo(1d);
        _surface.Margin = default;
        _fullLine.VisibleFrom = null;
    }

    /// <summary>
    /// Asks whether the pointer has left both the text and the line once it has settled: leaving one
    /// for the other raises the exit before the entry.
    /// </summary>
    private void CheckPointerLater()
    {
        if (_pointerCheckPending)
        {
            return;
        }

        _pointerCheckPending = true;
        Dispatcher.UIThread.Post(CheckPointer, DispatcherPriority.Input);
    }

    private void CheckPointer()
    {
        _pointerCheckPending = false;
        if (!_popup.IsOpen || IsPointerOver || _fullLine.IsPointerOver || _fullLine.IsHeld)
        {
            return;
        }

        // The line's context menu acts on it: it stays until the menu closes.
        if (_fullLine.ContextFlyout is { IsOpen: true } flyout && flyout.Target == _fullLine)
        {
            if (_awaitedFlyout is null)
            {
                _awaitedFlyout = flyout;
                flyout.Closed += OnFlyoutClosed;
            }

            return;
        }

        FadeOut();
    }

    private void OnFlyoutClosed(object? sender, EventArgs e)
    {
        if (_awaitedFlyout is not null)
        {
            _awaitedFlyout.Closed -= OnFlyoutClosed;
            _awaitedFlyout = null;
        }

        CheckPointerLater();
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
        _fullLine.SelectionStart = 0;
        _fullLine.SelectionEnd = 0;
        ShowAddedPartOnly();
        EnsureTransitions(duration);
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
            Conceal();
        }
    }

    /// <summary>The surface and the line's glyphs from the cut point on; the letters before it stay the text's.</summary>
    private void ShowAddedPartOnly()
    {
        var cut = CutPoint();
        _surface.Margin = new Thickness(cut, 0, 0, 0);
        _fullLine.VisibleFrom = cut;
    }

    private void EnsureTransitions(TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            _cover.Transitions ??= [new DoubleTransition { Property = FadingCover.FadeProperty, Duration = duration, Easing = new CubicEaseOut() }];
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
    /// The full line: selectable over its whole box, drawn from <see cref="VisibleFrom"/> on (or whole).
    /// The clip is drawn, not set on the visual, so it never narrows where the line takes the pointer.
    /// </summary>
    private sealed class FullLine(RevealingTextBlock owner) : SelectableTextBlock, ICustomHitTest
    {
        private double? _visibleFrom;

        protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

        /// <summary>Where the drawn part starts, from the line's left edge; null draws it whole.</summary>
        public double? VisibleFrom
        {
            get => _visibleFrom;
            set
            {
                _visibleFrom = value;
                InvalidateVisual();
            }
        }

        /// <summary>Whether a press on the line is still held (a selecting drag may leave its box).</summary>
        public bool IsHeld { get; set; }

        public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

        protected override void RenderTextLayout(DrawingContext context, Point origin)
        {
            if (_visibleFrom is not { } from)
            {
                base.RenderTextLayout(context, origin);
                return;
            }

            using (context.PushClip(new Rect(from, -Unbounded, Unbounded, 2 * Unbounded)))
            {
                base.RenderTextLayout(context, origin);
            }
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            owner.Reveal();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            owner.CheckPointerLater();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            owner.Settle();
            IsHeld = true;
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            IsHeld = false;
            owner.CheckPointerLater();
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            IsHeld = false;
            owner.CheckPointerLater();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            // The text's own scrolling host is not an ancestor of the overlay: the wheel goes on to
            // the text, which closes the line and passes it to whatever scrolls the card.
            base.OnPointerWheelChanged(e);
            owner.Conceal();
            if (!e.Handled)
            {
                owner.RaiseEvent(e);
            }
        }
    }

    /// <summary>
    /// The popup's content: the opaque surface, and the full line over it. One fade drives both, each
    /// as its own primitive.
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
