using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Krótki napis w przygaszonej kolumnie po prawej stronie pozycji menu - tej samej, w której menu
/// pokazuje skrót klawiszowy (np. kierunek sortowania przy wybranym polu w <see cref="SortPicker"/>).
/// Pozycja nie ma naraz skrótu i takiego napisu. Wygląd należy do motywu MenuItem
/// (Themes/DungeonControls.axaml, PART_TrailingText).
/// </summary>
public static class MenuItemTrailing
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, string?>("Text", typeof(MenuItemTrailing));

    public static string? GetText(MenuItem item) => item.GetValue(TextProperty);

    public static void SetText(MenuItem item, string? value) => item.SetValue(TextProperty, value);
}
