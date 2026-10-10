using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The frame of a marker row is the remove action: it keeps its bounds under the pointer and ignores a removed row.</summary>
public sealed class FramedIconRowRemoveTests
{
    private sealed class CountingCommand : ICommand
    {
        public int Runs { get; private set; }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Runs++;
    }

    private static (Window Window, FramedIconRow Row, Button Frame, CountingCommand Command) Build(bool removed)
    {
        var command = new CountingCommand();
        var row = new FramedIconRow { Title = "Oślepiony", Detail = "2 tury", RemoveCommand = command, IsRemoved = removed, Width = 400 };
        var window = new Window { Width = 500, Height = 300, Content = new StackPanel { Children = { row } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var frame = row.GetVisualDescendants().OfType<Button>().First();
        return (window, row, frame, command);
    }

    private static Point Centre(Window window, Control frame) =>
        frame.TranslatePoint(new Point(frame.Bounds.Width / 2, frame.Bounds.Height / 2), window)!.Value;

    [AvaloniaFact]
    public void Pointer_over_the_frame_keeps_its_bounds_and_a_click_removes()
    {
        var (window, _, frame, command) = Build(false);
        var before = frame.Bounds;
        var over = Centre(window, frame);

        window.MouseMove(over);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":pointerover", frame.Classes);
        Assert.Equal(before, frame.Bounds);

        window.MouseDown(over, MouseButton.Left);
        window.MouseUp(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, command.Runs);

        window.Close();
    }

    [AvaloniaFact]
    public void A_removed_row_cannot_be_removed_again()
    {
        var (window, _, frame, command) = Build(true);
        var over = Centre(window, frame);

        window.MouseMove(over);
        window.MouseDown(over, MouseButton.Left);
        window.MouseUp(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, command.Runs);
        Assert.DoesNotContain(":pointerover", frame.Classes);

        window.Close();
    }
}
