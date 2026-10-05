using System.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// DropDownPicker with object items: names in the rows and closed-list text come from
/// DisplayMemberBinding, not ToString().
/// </summary>
public sealed class DropDownPickerDisplayMemberTests
{
    private sealed class Option(string name)
    {
        public string Name { get; } = name;

        public override string ToString() => "nie ta nazwa";
    }

    [AvaloniaFact]
    public void Rows_and_summary_show_the_name_from_the_display_member_binding()
    {
        Option[] options = [new("Pierwsza"), new("Druga"), new("Trzecia")];
        var picker = new DropDownPicker
        {
            DisplayMemberBinding = CompiledBinding.Create<Option, string>(option => option.Name),
            ItemsSource = options,
            SelectedItems = new AvaloniaList<object> { options[0], options[2] },
        };

        Assert.Equal(["Pierwsza", "Druga", "Trzecia"], picker.Rows.Select(row => row.Text));
        Assert.Equal("Pierwsza", picker.LabelText);
        Assert.Equal("+1", picker.MoreBadgeText);
    }
}
