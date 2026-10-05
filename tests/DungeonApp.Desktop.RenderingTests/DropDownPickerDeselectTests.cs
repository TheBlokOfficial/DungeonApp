using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Single selection: in chip form (a filter where nothing means everything), clicking the selected
/// value again deselects it and closes the flyout; a regular form drop-down keeps its selection.
/// </summary>
public sealed class DropDownPickerDeselectTests
{
    [AvaloniaTheory]
    [InlineData(true, null)]
    [InlineData(false, "Średni")]
    public void Clicking_the_chosen_value_again(bool chip, string? expected)
    {
        var picker = new DropDownPicker
        {
            SelectionMode = DropDownPickerMode.Single,
            ItemsSource = new[] { "Mały", "Średni", "Duży" },
            SelectedItem = "Średni",
        };
        if (chip)
        {
            picker.Theme = (ControlTheme)Application.Current!.FindResource("DungeonChipPicker")!;
        }

        var window = new Window { Width = 400, Height = 300, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        picker.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        picker.ChooseSingle("Średni");

        Assert.Equal(expected, picker.SelectedItem);
        Assert.False(picker.IsDropDownOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Chip_still_switches_to_another_value()
    {
        var picker = new DropDownPicker
        {
            SelectionMode = DropDownPickerMode.Single,
            AllowsDeselect = true,
            ItemsSource = new[] { "Mały", "Średni", "Duży" },
            SelectedItem = "Średni",
        };

        picker.ChooseSingle("Duży");

        Assert.Equal("Duży", picker.SelectedItem);
    }
}
