using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The sort picker's menu checkmark reflects the selection: clicking the selected field again must
/// keep the checkmark (a checkable item toggles itself before its click handler runs).
/// </summary>
public sealed class SortPickerMenuTests
{
    [AvaloniaFact]
    public void Clicking_the_selected_field_again_keeps_its_check()
    {
        var picker = new SortPicker
        {
            Options = ["Nazwa", "Wyzwanie"],
            SelectedOption = "Wyzwanie",
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var window = new Window { Width = 600, Height = 400, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var link = picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FieldButton");
        var linkPoint = link.TranslatePoint(new Point(5, 5), window)!.Value;
        window.MouseDown(linkPoint, MouseButton.Left);
        window.MouseUp(linkPoint, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        var items = ((MenuFlyout)link.Flyout!).Items.OfType<MenuItem>().ToArray();
        var popup = TopLevel.GetTopLevel(items[1])!;
        var itemPoint = items[1].TranslatePoint(new Point(20, 10), popup)!.Value;
        popup.MouseDown(itemPoint, MouseButton.Left);
        popup.MouseUp(itemPoint, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Wyzwanie", picker.SelectedOption);
        Assert.Equal(new[] { false, true }, items.Select(item => item.IsChecked).ToArray());
        window.Close();
    }
}
