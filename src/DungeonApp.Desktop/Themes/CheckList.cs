using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Marker for lists whose rows contain checkboxes (ListBox with DungeonCheckList theme,
/// DropDownPicker in multiple-selection mode). Inherits down the tree - set on a list,
/// applies to its rows. The list-row theme (ListBoxItem in Themes/Controls/ListBox.axaml) reads it:
/// only the checkbox indicates row state, selected rows have no accent background, and hover
/// looks like a regular row. Changes appearance only.
/// </summary>
public static class CheckList
{
    public static readonly AttachedProperty<bool> IsCheckListProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsCheckList", typeof(CheckList), inherits: true);

    public static bool GetIsCheckList(Control control) => control.GetValue(IsCheckListProperty);

    public static void SetIsCheckList(Control control, bool value) => control.SetValue(IsCheckListProperty, value);
}
