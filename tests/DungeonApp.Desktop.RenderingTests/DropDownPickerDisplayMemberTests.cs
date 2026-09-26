using System.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// DropDownPicker na obiektach: nazwa pozycji w wierszach i w tekście zamkniętej listy pochodzi
/// z DisplayMemberBinding, nie z ToString().
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
        Assert.Equal(DropDownPickerText.Summary(["Pierwsza", "Trzecia"]), picker.SummaryText);
    }
}
