using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Invisible but measured bold copy of label text (tab, segment). Sits in the template
/// beneath visible text in the same panel and determines width in every state - bolding
/// the selected label therefore does not change its width or move neighbours.
/// Works only for text content: non-text content (control) cannot have two
/// parents, so no copy is created and no space is reserved.
/// </summary>
public sealed class BoldTextReserve : TextBlock
{
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<BoldTextReserve, object?>(nameof(Label));

    // Local values, not defaults: weight and wrapping are inherited, so a default
    // would lose to label weight (Normal at rest) and the copy would not be bold.
    public BoldTextReserve()
    {
        FontWeight = FontWeight.SemiBold;
        Opacity = 0;
        IsHitTestVisible = false;
        TextWrapping = TextWrapping.NoWrap;
        TextTrimming = TextTrimming.CharacterEllipsis;
    }

    public object? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty)
        {
            Text = change.NewValue as string;
        }
    }
}
