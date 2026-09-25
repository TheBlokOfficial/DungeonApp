namespace DungeonApp.Desktop.Controls;

/// <summary>
/// One row of an open <see cref="DropDownPicker"/>. Rebuilt whenever the items, the selection or the
/// search change, so it carries no change notification.
/// </summary>
public sealed class DropDownPickerRow(object item, string text, bool isSelected, bool isMultiple)
{
    public object Item { get; } = item;

    public string Text { get; } = text;

    public bool IsSelected { get; } = isSelected;

    /// <summary>Multiple mode: a check box before the text says the row is selected.</summary>
    public bool IsMultiple { get; } = isMultiple;

    /// <summary>Single mode only: the selected row has the dim accent background of a list row.</summary>
    public bool ShowsSelectedBackground => IsSelected && !IsMultiple;
}
