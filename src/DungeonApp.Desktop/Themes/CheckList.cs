using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Znacznik listy, której wiersze mają pole wyboru (ListBox z motywem DungeonCheckList, wiersze
/// DropDownPicker w trybie wielokrotnym). Dziedziczy się w dół drzewa - ustawiony na liście
/// obejmuje jej wiersze. Motyw wiersza listy (ListBoxItem w Themes/DungeonControls.axaml) czyta go:
/// stan wiersza niesie wtedy samo pole wyboru, wybrany wiersz nie dostaje tła akcentu, a najechanie
/// wygląda jak w zwykłym wierszu. Zmienia wyłącznie wygląd.
/// </summary>
public static class CheckList
{
    public static readonly AttachedProperty<bool> IsCheckListProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsCheckList", typeof(CheckList), inherits: true);

    public static bool GetIsCheckList(Control control) => control.GetValue(IsCheckListProperty);

    public static void SetIsCheckList(Control control, bool value) => control.SetValue(IsCheckListProperty, value);
}
