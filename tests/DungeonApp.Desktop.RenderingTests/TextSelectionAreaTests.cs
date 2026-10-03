using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Selecting by a drag that starts beside the text, driven by real pointer input in a window: the
/// press lands on the area, not on the text, and the selection still follows the drag.
/// </summary>
public sealed class TextSelectionAreaTests
{
    private const string Sentence = "Ugryzienie wilka";

    private static Window Show(Control content)
    {
        var window = new Window { Width = 800, Height = 400, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static TextSelectionArea Area(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new TextSelectionArea
        {
            Width = 600,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = panel,
        };
    }

    private static SelectableTextBlock Text() => new()
    {
        Text = Sentence,
        Margin = new Thickness(60, 40, 0, 0),
        HorizontalAlignment = HorizontalAlignment.Left,
    };

    [AvaloniaFact]
    public void A_drag_started_left_of_a_text_and_run_past_its_end_selects_all_of_it()
    {
        var text = Text();
        var window = Show(Area(text));
        var y = 40 + (text.Bounds.Height / 2);

        window.MouseDown(new Point(10, y), MouseButton.Left);
        window.MouseMove(new Point(590, y));
        window.MouseUp(new Point(590, y), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Sentence, text.SelectedText);
        Assert.True(text.IsFocused);

        window.Close();
    }

    [AvaloniaFact]
    public void A_press_on_a_button_in_the_area_is_left_to_the_button()
    {
        var text = Text();
        var button = new Button { Content = "Akcja", Height = 32 };
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var window = Show(Area(button, text));
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

        window.MouseDown(center, MouseButton.Left);
        window.MouseMove(new Point(590, center.Y + 60));
        window.MouseUp(new Point(590, center.Y + 60), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(string.Empty, text.SelectedText);
        Assert.False(text.IsFocused);

        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        window.Close();
    }
}
