using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// A row of a marker on a card (a condition, an effect): a framed square with an icon, the title,
/// a muted line under it (modifiers; turns and a note) and a remove button at the end. The muted line
/// is optional and the row keeps its height without it. A removed row stays where it is, dimmed and
/// without the button, so nothing moves until the change is settled. The look belongs to the frame's
/// control theme (Themes/Controls/FramedIconRow.axaml).
/// </summary>
public sealed class FramedIconRow : TemplatedControl
{
    public static readonly StyledProperty<DrawingImage?> IconProperty =
        AvaloniaProperty.Register<FramedIconRow, DrawingImage?>(nameof(Icon));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<FramedIconRow, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DetailProperty =
        AvaloniaProperty.Register<FramedIconRow, string?>(nameof(Detail));

    public static readonly StyledProperty<bool> IsRemovedProperty =
        AvaloniaProperty.Register<FramedIconRow, bool>(nameof(IsRemoved));

    public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
        AvaloniaProperty.Register<FramedIconRow, ICommand?>(nameof(RemoveCommand));

    static FramedIconRow()
    {
        IsRemovedProperty.Changed.AddClassHandler<FramedIconRow>((row, args) =>
            row.PseudoClasses.Set(":removed", args.GetNewValue<bool>()));
    }

    /// <summary>The icon inside the frame; without it the frame stays empty.</summary>
    public DrawingImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The muted line under the title; its space is not reserved when empty.</summary>
    public string? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    /// <summary>A removed row that is not settled yet: dimmed, its button hidden.</summary>
    public bool IsRemoved
    {
        get => GetValue(IsRemovedProperty);
        set => SetValue(IsRemovedProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
}
