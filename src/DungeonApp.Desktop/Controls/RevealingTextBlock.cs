using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Selectable text that, when trimmed, shows itself whole in place while the pointer is over it -
/// like a trimmed name in the Windows Explorer tree: the full line opens exactly over the trimmed
/// one, in the same type, unwrapped, on the background the text stands on, and may run past the
/// panel's edge. Nothing opens when the text is not trimmed (wrapping text never is).
/// <para>
/// It is a <see cref="SelectableTextBlock"/> to styles and themes (<see cref="StyleKeyOverride"/>), so
/// the typography classes and the frame's theme apply to it unchanged; trimming and wrapping are set
/// by the host as on any text block. The full line lives in a popup on the window's overlay layer
/// that takes no pointer input: the pointer stays over the text underneath, so the popup cannot make
/// it leave and come back (flicker), and selecting still works. A press, the wheel or leaving the
/// text closes it.
/// </para>
/// </summary>
public class RevealingTextBlock : SelectableTextBlock, ICustomHitTest
{
    private readonly TextBlock _fullText;
    private readonly Border _surface;
    private readonly Popup _popup;

    public RevealingTextBlock()
    {
        _fullText = new TextBlock { TextWrapping = TextWrapping.NoWrap };
        _surface = new Border
        {
            Child = _fullText,
            IsHitTestVisible = false,
        };
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

        // A logical child, so the popup's content resolves the window's styles and resources.
        LogicalChildren.Add(_popup);
    }

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    /// <summary>Whether the text is drawn shortened: some line of it ends in the trimming mark.</summary>
    public bool IsTrimmed => TextLayout.TextLines.Any(line => line.HasCollapsed);

    /// <summary>Whether the full text stands over the trimmed one now.</summary>
    public bool IsRevealed => _popup.IsOpen;

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
        Conceal();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // Selecting works on the trimmed text underneath, so the cover steps aside for it.
        Conceal();
        base.OnPointerPressed(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // The overlay does not scroll with the text: it would be left behind.
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
        _surface.Background = BackgroundBehind();
        _popup.IsOpen = true;
    }

    public void Conceal() => _popup.IsOpen = false;

    /// <summary>
    /// The background the text stands on - its own, else the nearest ancestor's - so the full line
    /// covers the trimmed one as if it were written on the same surface.
    /// </summary>
    private IBrush? BackgroundBehind()
    {
        if (Background is not null)
        {
            return Background;
        }

        foreach (var ancestor in this.GetVisualAncestors())
        {
            var brush = ancestor switch
            {
                Border border => border.Background,
                Panel panel => panel.Background,
                TemplatedControl control => control.Background,
                ContentPresenter presenter => presenter.Background,
                _ => null,
            };
            if (brush is not null)
            {
                return brush;
            }
        }

        return null;
    }
}
