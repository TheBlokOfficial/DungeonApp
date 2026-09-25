using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Niewidoczna, ale mierzona pogrubiona kopia napisu etykiety (zakładka, segment). Leży w szablonie
/// pod widocznym napisem w tym samym panelu i wyznacza jego szerokość w każdym stanie - pogrubienie
/// wybranej etykiety nie zmienia więc jej szerokości i nie przesuwa sąsiadów.
/// Działa wyłącznie dla treści tekstowej: treść niebędąca tekstem (kontrolka) nie może mieć dwóch
/// rodziców, więc kopii nie ma i miejsca się nie rezerwuje.
/// </summary>
public sealed class BoldTextReserve : TextBlock
{
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<BoldTextReserve, object?>(nameof(Label));

    static BoldTextReserve()
    {
        FontWeightProperty.OverrideDefaultValue<BoldTextReserve>(FontWeight.SemiBold);
        OpacityProperty.OverrideDefaultValue<BoldTextReserve>(0);
        IsHitTestVisibleProperty.OverrideDefaultValue<BoldTextReserve>(false);
        TextWrappingProperty.OverrideDefaultValue<BoldTextReserve>(TextWrapping.NoWrap);
        TextTrimmingProperty.OverrideDefaultValue<BoldTextReserve>(TextTrimming.CharacterEllipsis);
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
