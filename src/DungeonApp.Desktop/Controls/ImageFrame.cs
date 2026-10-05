using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A frame for one picture, in exactly one of three states, chosen by what it is given:
/// <list type="bullet">
/// <item><b>picture</b> - <see cref="Source"/> is set: the picture fills the frame, cropped to the
/// frame's proportions, never stretched out of shape (scaled down smoothly, so a large portrait
/// has no stair-steps);</item>
/// <item><b>placeholder</b> - no <see cref="Source"/> and no <see cref="Message"/>: <see cref="Icon"/>,
/// large and dimmed, in the middle;</item>
/// <item><b>error</b> - no <see cref="Source"/>, a <see cref="Message"/>: the same icon, under it the
/// message in the danger color and, secondary to it, its <see cref="Detail"/>.</item>
/// </list>
/// It knows nothing about where a picture comes from - whoever places the frame decides the state and
/// hands it ready data. It has no size of its own: its size and proportions (square, portrait) come
/// from the layout it stands in. The look belongs to the frame's control theme
/// (Themes/Controls/ImageFrame.axaml).
/// <para>
/// A <see cref="Source"/> that is a <see cref="DrawingImage"/> is a one-colour shape (a vector icon),
/// not a picture to fill the frame: it is drawn exactly where and as large as the placeholder icon,
/// in the same dimmed foreground, so a frame with the entry's own icon and one with the system's
/// placeholder look alike, and the same file reads right on any theme.
/// </para>
/// </summary>
public sealed class ImageFrame : TemplatedControl
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ImageFrame, IImage?>(nameof(Source));

    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<ImageFrame, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ImageFrame, string?>(nameof(Message));

    public static readonly StyledProperty<string?> DetailProperty =
        AvaloniaProperty.Register<ImageFrame, string?>(nameof(Detail));

    /// <summary>What the template's icon place draws: the vector <see cref="Source"/>, else <see cref="Icon"/>.</summary>
    public static readonly StyledProperty<DrawingImage?> ShownIconProperty =
        AvaloniaProperty.Register<ImageFrame, DrawingImage?>(nameof(ShownIcon));

    public ImageFrame()
    {
        UpdateState();
    }

    /// <summary>The picture. Set: the frame shows it, whatever else is set.</summary>
    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>The icon shown in place of a picture - the kind of thing the picture would show.</summary>
    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Why there is no picture although there should be one. Set: the error state.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>What the error is about - a file path, say. Shown under <see cref="Message"/>.</summary>
    public string? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    /// <summary>The drawing the template's icon place shows; for the theme's template, not for callers.</summary>
    public DrawingImage? ShownIcon
    {
        get => GetValue(ShownIconProperty);
        private set => SetValue(ShownIconProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty || change.Property == MessageProperty || change.Property == IconProperty)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        var hasPicture = Source is not null;
        var vector = Source as DrawingImage;
        PseudoClasses.Set(":picture", hasPicture);
        PseudoClasses.Set(":vector", vector is not null);
        PseudoClasses.Set(":error", !hasPicture && !string.IsNullOrEmpty(Message));
        ShownIcon = vector ?? Icon;
    }
}
