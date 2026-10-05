using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A frame for one picture, in exactly one of three states, chosen by what it is given:
/// <list type="bullet">
/// <item><b>picture</b> - <see cref="Source"/> is set: the picture fills the frame, cropped to the
/// frame's proportions, never stretched out of shape;</item>
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
/// A picture that is a one-colour icon (<see cref="SourceIsMask"/>) is not drawn in its own colours:
/// only its transparency is read, and the theme paints it in an icon colour of its own, so the same
/// file reads right on any theme.
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

    public static readonly StyledProperty<bool> SourceIsMaskProperty =
        AvaloniaProperty.Register<ImageFrame, bool>(nameof(SourceIsMask));

    private Border? _mask;

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

    /// <summary>
    /// Whether <see cref="Source"/> is a one-colour icon: its shape is shown, centred and whole,
    /// in the theme's icon colour, instead of the picture filling the frame in its own colours.
    /// </summary>
    public bool SourceIsMask
    {
        get => GetValue(SourceIsMaskProperty);
        set => SetValue(SourceIsMaskProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _mask = e.NameScope.Find<Border>("PART_Mask");
        UpdateMask();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty || change.Property == MessageProperty || change.Property == SourceIsMaskProperty)
        {
            UpdateState();
            UpdateMask();
        }
    }

    private void UpdateState()
    {
        var hasPicture = Source is not null;
        PseudoClasses.Set(":picture", hasPicture);
        PseudoClasses.Set(":mask", hasPicture && SourceIsMask);
        PseudoClasses.Set(":error", !hasPicture && !string.IsNullOrEmpty(Message));
    }

    /// <summary>
    /// The icon's shape cuts the theme's colour out of a plain fill: the picture becomes the fill's
    /// opacity mask. Set from code because the mask takes a brush source, which a picture handed to
    /// <see cref="Source"/> is only when it is a bitmap; any other picture gives no shape, so the fill
    /// stays hidden rather than painting the whole frame.
    /// </summary>
    private void UpdateMask()
    {
        if (_mask is null)
        {
            return;
        }

        _mask.OpacityMask = Source is IImageBrushSource shape
            ? new ImageBrush(shape) { Stretch = Stretch.Uniform }
            : Brushes.Transparent;
    }
}
