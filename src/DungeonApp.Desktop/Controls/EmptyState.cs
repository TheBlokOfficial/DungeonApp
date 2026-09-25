using Avalonia;
using Avalonia.Controls.Primitives;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// What an empty list shows in place of its rows: a message and, optionally, a hint under it saying
/// what to do. It stands at the top of the list's area, not in the middle of a tall list; no picture
/// and no icon. The look belongs to the frame's control theme (Themes/DungeonControls.axaml).
/// </summary>
public sealed class EmptyState : TemplatedControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Hint));

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }
}
