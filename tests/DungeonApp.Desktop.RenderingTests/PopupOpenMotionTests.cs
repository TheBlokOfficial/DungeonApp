using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// A drop-down with no room below opens above the field, and the field gets the opens-up class
/// (the theme uses it to turn the arrow upward); with room below, it gets no class.
/// </summary>
public sealed class PopupOpenMotionTests
{
    [AvaloniaTheory]
    [InlineData(VerticalAlignment.Top, false)]
    [InlineData(VerticalAlignment.Bottom, true)]
    public void Opener_knows_whether_its_list_opened_above_it(VerticalAlignment alignment, bool opensUp)
    {
        PopupOpenMotion.Register();
        var combo = new ComboBox { Width = 200, VerticalAlignment = alignment, ItemsSource = new[] { "a", "b", "c", "d", "e" } };
        var window = new Window { Width = 400, Height = 300, Content = combo };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        combo.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(opensUp, combo.Classes.Contains(PopupOpenMotion.OpensUpClass));
        window.Close();
    }
}
