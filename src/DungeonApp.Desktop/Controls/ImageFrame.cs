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
/// <item><b>error</b> - no <see cref="Source"/>, a <see cref="Message"/>: the same icon, and under it the
/// message and its <see cref="Detail"/> in the danger color.</item>
/// </list>
/// It knows nothing about where a picture comes from - whoever places the frame decides the state and
/// hands it ready data. It has no size of its own: its size and proportions (square, portrait) come
/// from the layout it stands in. The look belongs to the frame's control theme
/// (Themes/DungeonControls.axaml).
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty || change.Property == MessageProperty)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        var hasPicture = Source is not null;
        PseudoClasses.Set(":picture", hasPicture);
        PseudoClasses.Set(":error", !hasPicture && !string.IsNullOrEmpty(Message));
    }
}
