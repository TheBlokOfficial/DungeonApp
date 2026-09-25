using System.Linq;
using Avalonia.Collections;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Tests;

// A change of the selection keeps the row objects, so the open list does not rebuild its rows (and
// their pointer-over look) on every click; only the items, the mode or the search build new rows.
public sealed class DropDownPickerRowsTests
{
    [Fact]
    public void Selecting_in_multiple_mode_keeps_the_rows_and_marks_the_selected_one()
    {
        var selected = new AvaloniaList<object>();
        var picker = new DropDownPicker { ItemsSource = new[] { "Humanoid", "Smok" }, SelectedItems = selected };
        var rowsBefore = picker.Rows;

        selected.Add("Smok");

        Assert.Same(rowsBefore, picker.Rows);
        Assert.Equal([false, true], picker.Rows.Select(row => row.IsSelected));
        Assert.Equal("Smok", picker.SummaryText);
    }

    [Fact]
    public void Selecting_in_single_mode_keeps_the_rows_and_moves_the_selected_background()
    {
        var picker = new DropDownPicker
        {
            SelectionMode = DropDownPickerMode.Single,
            ItemsSource = new[] { "Mały", "Duży" },
            SelectedItem = "Mały",
        };
        var rowsBefore = picker.Rows;

        picker.SelectedItem = "Duży";

        Assert.Same(rowsBefore, picker.Rows);
        Assert.Equal([false, true], picker.Rows.Select(row => row.ShowsSelectedBackground));
    }

    [Fact]
    public void Searching_builds_the_rows_the_search_keeps()
    {
        var picker = new DropDownPicker { ItemsSource = new[] { "Humanoid", "Smok" } };

        picker.SearchText = "smo";

        Assert.Equal(["Smok"], picker.Rows.Select(row => row.Text));
    }
}
