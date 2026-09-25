using System.ComponentModel;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// One row of an open <see cref="DropDownPicker"/>. Built when the items, the mode or the search
/// change; a change of the selection keeps the row object and only notifies its selection state,
/// so the row's container - and its pointer-over look - stays in place.
/// </summary>
public sealed class DropDownPickerRow(object item, string text, bool isSelected, bool isMultiple)
    : INotifyPropertyChanged
{
    private bool _isSelected = isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object Item { get; } = item;

    public string Text { get; } = text;

    public bool IsSelected
    {
        get => _isSelected;
        internal set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowsSelectedBackground)));
        }
    }

    /// <summary>Multiple mode: a check box before the text says the row is selected.</summary>
    public bool IsMultiple { get; } = isMultiple;

    /// <summary>Single mode only: the selected row has the dim accent background of a list row.</summary>
    public bool ShowsSelectedBackground => IsSelected && !IsMultiple;
}
