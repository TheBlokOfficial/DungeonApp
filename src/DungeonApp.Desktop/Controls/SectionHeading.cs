using Avalonia;
using Avalonia.Controls.Primitives;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// The heading of a section of a card's text ("Akcje", "Reakcje"): a short accent bar and the
/// section's title. It marks where a section starts so the eye can jump between them - it opens
/// and closes nothing. The look belongs to the frame's control theme
/// (Themes/Controls/SectionHeading.axaml).
/// </summary>
public sealed class SectionHeading : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SectionHeading, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
