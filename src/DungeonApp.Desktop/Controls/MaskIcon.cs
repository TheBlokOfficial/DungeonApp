using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Draws a one-colour picture (a shape on a transparent background, such as a pack's icon) in the
/// foreground colour it is given, not in the picture's own: only the picture's transparency is read,
/// so the same file reads right on any theme. The shape is shown whole and centred (Uniform). The
/// twin of <see cref="ForegroundIcon"/>, for a bitmap instead of a drawing from the icon set. A
/// picture that is not a bitmap gives no shape and nothing is drawn.
/// </summary>
public sealed class MaskIcon : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<MaskIcon, IImage?>(nameof(Source));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<MaskIcon>();

    static MaskIcon()
    {
        AffectsRender<MaskIcon>(SourceProperty, ForegroundProperty);
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (Source is not IImageBrushSource shape || Foreground is not { } foreground || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // The shape cuts the colour out of a plain fill: the picture is the fill's opacity mask.
        using (context.PushOpacityMask(new ImageBrush(shape) { Stretch = Stretch.Uniform }, bounds))
        {
            context.FillRectangle(foreground, bounds);
        }
    }
}
