using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Media;
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
/// by the host as on any text block. The full line lives in a popup on the window's overlay layer
/// that takes no pointer input: the pointer stays over the text underneath, so the popup cannot make
/// it leave and come back (flicker), and selecting still works. Leaving the text fades it out; a
/// press, the wheel, a new text or a new size close it at once.
/// </para>
/// <para>
/// The surface is opaque, so while it fades in it covers the trimmed text by exactly its own
/// opacity - a cross-fade at constant brightness, never two texts added together. Only once it is
/// fully opaque is the trimmed text left undrawn, and it is drawn again before the surface starts
/// fading out: there is no frame with two texts at once nor one with none. With animations off in
/// Windows (<see cref="Themes.SystemMotion.IsReduced"/>) it opens and closes at once.
/// </para>
/// </summary>
public class RevealingTextBlock : SelectableTextBlock, ICustomHitTest
{
    private const string SurfaceBrushKey = "DungeonBackstageCardBrush";

    private readonly TextBlock _fullText;
    private readonly Border _surface;
    private readonly Popup _popup;
    private readonly DispatcherTimer _fadeEnd;
    private bool _revealed;
    private bool _trimmedTextHidden;

    public RevealingTextBlock()
    {
        _fullText = new TextBlock { TextWrapping = TextWrapping.NoWrap };
        _surface = new Border
        {
            Child = _fullText,
            IsHitTestVisible = false,
        };
        _surface.Bind(Border.BackgroundProperty, _surface.GetResourceObservable(SurfaceBrushKey));
        _popup = new Popup
        {
            Child = _surface,
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
    /// Whether the trimmed text underneath is left undrawn: while the full line stands opaque over it.
    /// Its place in the layout never changes.
    /// </summary>
    public bool IsTrimmedTextHidden => _trimmedTextHidden;

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

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        if (!_trimmedTextHidden)
        {
            base.RenderTextLayout(context, origin);
        }
    }

    /// <summary>Opens the full line over the text when the text is trimmed; otherwise does nothing.</summary>
    public void Reveal()
    {
        if (!IsTrimmed || string.IsNullOrEmpty(Text) || TopLevel.GetTopLevel(this) is null)
        {
            return;
        }

        _fullText.Text = Text;
        _fullText.FontFamily = FontFamily;
        _fullText.FontSize = FontSize;
        _fullText.FontWeight = FontWeight;
        _fullText.FontStyle = FontStyle;
        _fullText.FontStretch = FontStretch;
        _fullText.FontFeatures = FontFeatures;
        _fullText.LetterSpacing = LetterSpacing;
        _fullText.LineHeight = LineHeight;
        _fullText.Foreground = Foreground;
        _fullText.Padding = Padding;

        _revealed = true;
        _fadeEnd.Stop();
        var duration = FadeDuration();

        if (!_popup.IsOpen)
        {
            // Without transitions, so the fade starts from nothing rather than from where the last one
            // was cut short.
            _surface.Transitions = null;
            _surface.Opacity = duration > TimeSpan.Zero ? 0d : 1d;
            _popup.IsOpen = true;
        }

        if (duration > TimeSpan.Zero)
        {
            _surface.Transitions ??= [new DoubleTransition { Property = OpacityProperty, Duration = duration, Easing = new CubicEaseOut() }];
            _surface.Opacity = 1d;
            _fadeEnd.Interval = duration;
            _fadeEnd.Start();
        }
        else
        {
            SetTrimmedTextHidden(true);
        }
    }

    /// <summary>Closes the full line at once, with no fade.</summary>
    public void Conceal()
    {
        _revealed = false;
        _fadeEnd.Stop();
        SetTrimmedTextHidden(false);
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
        _fadeEnd.Stop();

        // Drawn again under the still opaque surface, which uncovers it as it fades.
        SetTrimmedTextHidden(false);
        _surface.Opacity = 0d;
        _fadeEnd.Interval = duration;
        _fadeEnd.Start();
    }

    private void OnFadeEnd(object? sender, EventArgs e)
    {
        _fadeEnd.Stop();
        if (_revealed)
        {
            SetTrimmedTextHidden(true);
        }
        else
        {
            _popup.IsOpen = false;
        }
    }

    private void SetTrimmedTextHidden(bool hidden)
    {
        if (_trimmedTextHidden != hidden)
        {
            _trimmedTextHidden = hidden;
            InvalidateVisual();
        }
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
}
