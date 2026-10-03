using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A headline value as a borderless column of text: a label with its icon, the value large, a note
/// under it ("pancerz naturalny", "2k8+2"). Values of one row stand side by side in equal-width
/// columns the host lays out, so a long note wraps inside its column instead of pushing the row
/// apart. Nothing to click: every
/// text in it is selectable like the rest of a card. The look belongs to the frame's control theme
/// (Themes/Controls/StatTile.axaml).
/// </summary>
public sealed class StatTile : TemplatedControl
{
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<StatTile, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<StatTile, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<StatTile, string?>(nameof(Value));

    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<StatTile, string?>(nameof(Note));

    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Shown under the value only when set.</summary>
    public string? Note
    {
        get => GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }
}
