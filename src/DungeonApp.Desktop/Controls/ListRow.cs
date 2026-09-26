using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Wiersz listy, który jest poleceniem: przycisk o wyglądzie wiersza listy ramy (ListBoxItem) -
/// do list zbudowanych jako ItemsControl, gdzie wybór albo otwarcie niesie polecenie modelu widoku,
/// nie zaznaczenie kontrolki. <see cref="IsSelected"/> (pseudoklasa :selected) podaje model widoku;
/// wiersz sam się nie zaznacza. <see cref="ShowsSelectionStripe"/> włącza kreskę zaznaczenia w pasie
/// wcięcia po lewej stronie wiersza. Wygląd, stany, wysokość i pas kreski należą do motywu ramy
/// (Themes/DungeonControls.axaml); szerokość pasa i wcięcia kreski podaje układający listę zasobami
/// DungeonListRowStripeGutter i DungeonListRowStripeInset na poziomie listy.
/// </summary>
public sealed class ListRow : Button
{
    /// <summary>Czy wiersz jest wybrany (pseudoklasa :selected).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<ListRow, bool>(nameof(IsSelected));

    /// <summary>Czy wiersz ma pas wcięcia z kreską zaznaczenia (domyślnie nie).</summary>
    public static readonly StyledProperty<bool> ShowsSelectionStripeProperty =
        AvaloniaProperty.Register<ListRow, bool>(nameof(ShowsSelectionStripe));

    static ListRow()
    {
        IsSelectedProperty.Changed.AddClassHandler<ListRow>((row, e) =>
            row.PseudoClasses.Set(":selected", e.GetNewValue<bool>()));
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowsSelectionStripe
    {
        get => GetValue(ShowsSelectionStripeProperty);
        set => SetValue(ShowsSelectionStripeProperty, value);
    }
}
