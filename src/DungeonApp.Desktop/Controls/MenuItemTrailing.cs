using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Short text in the muted right-hand column of a menu item - the same column used for
/// keyboard shortcuts (e.g. sort direction beside the selected field in <see cref="SortPicker"/>).
/// An item never has both a shortcut and this text. Appearance belongs to the MenuItem theme
/// (Themes/Controls/Menus.axaml, PART_TrailingText).
/// </summary>
public static class MenuItemTrailing
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, string?>("Text", typeof(MenuItemTrailing));

    public static string? GetText(MenuItem item) => item.GetValue(TextProperty);

    public static void SetText(MenuItem item, string? value) => item.SetValue(TextProperty, value);
}
