using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The open list is as wide as its widest item from the start, also when that item is far down the
/// list, where virtualisation or lazy row creation would not have reached it yet.
/// </summary>
public sealed class DropDownPickerListWidthTests
{
    private const string Widest = "Bardzo długa nazwa pozycji, która leży daleko na końcu listy";

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void List_width_fits_the_widest_item_that_is_far_below_the_first_screen(bool chip)
    {
        var withLongItem = OpenedListWidth(chip, ItemsWith(Widest));
        var shortOnly = OpenedListWidth(chip, ItemsWith(null));

        Assert.True(
            withLongItem > shortOnly + 100,
            $"list with the long item far down is {withLongItem}, short-only list {shortOnly}");
    }

    [AvaloniaFact]
    public void Chip_opener_list_width_fits_the_widest_item_far_below_the_first_screen()
    {
        var withLongItem = OpenedFlyoutWidth(ItemsWith(Widest));
        var shortOnly = OpenedFlyoutWidth(ItemsWith(null));

        Assert.True(
            withLongItem > shortOnly + 100,
            $"list with the long item far down is {withLongItem}, short-only list {shortOnly}");
    }

    private static double OpenedFlyoutWidth(string[] items)
    {
        PopupOpenLayout.Register();
        var list = new ListBox { ItemsSource = items };
        var flyout = new Flyout
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            FlyoutPresenterClasses = { "list-host" },
            Content = list,
        };
        var button = new Button
        {
            Theme = (ControlTheme)Application.Current!.FindResource("DungeonChipOpener")!,
            Content = "Filtr",
            Flyout = flyout,
        };
        var window = new Window { Width = 900, Height = 600, Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        flyout.ShowAt(button);
        Dispatcher.UIThread.RunJobs();

        var width = list.GetVisualAncestors().OfType<FlyoutPresenter>().First().Bounds.Width;
        window.Close();
        return width;
    }

    private static string[] ItemsWith(string? longItem)
    {
        var items = Enumerable.Range(1, 120).Select(number => $"Pozycja {number}").ToArray();
        if (longItem is not null)
        {
            items[110] = longItem;
        }

        return items;
    }

    private static double OpenedListWidth(bool chip, string[] items)
    {
        PopupOpenLayout.Register();
        var picker = new DropDownPicker { ItemsSource = items, PlaceholderText = "Filtr", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        if (chip)
        {
            picker.Theme = (ControlTheme)Application.Current!.FindResource("DungeonChipPicker")!;
        }

        var window = new Window { Width = 900, Height = 600, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        picker.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
        var width = ((Control)popup.Child!).Bounds.Width;
        window.Close();
        return width;
    }
}
