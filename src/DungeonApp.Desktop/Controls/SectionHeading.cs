using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// The heading of a section of a card's text ("Akcje", "Reakcje"): a short accent bar, the section's
/// icon and its title. It marks where a section starts so the eye can jump between them - it opens
/// and closes nothing. The look belongs to the frame's control theme
/// (Themes/Controls/SectionHeading.axaml).
/// </summary>
public sealed class SectionHeading : TemplatedControl
{
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<SectionHeading, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SectionHeading, string?>(nameof(Text));

    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
