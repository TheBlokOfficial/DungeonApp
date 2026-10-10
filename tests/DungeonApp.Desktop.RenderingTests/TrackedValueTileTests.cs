using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The tracked value's two forms - the number to read and the change field opened by a click - take
/// exactly the same place: neither the tile, its number nor a neighbouring tile moves or changes size
/// when the field opens or while a change is typed. Enter passes on a valid change and nothing else.
/// </summary>
public sealed class TrackedValueTileTests
{
    private sealed record Sample(Window Window, TrackedValueTile Tile, StatTile Neighbour, List<NumberChange> Accepted);

    private static Sample Show()
    {
        var tile = new TrackedValueTile { Label = "PW", Value = "9", Maximum = "13", Note = "2k8+4" };
        var neighbour = new StatTile { Label = "KP", Value = "15", Note = "skórznia" };
        Grid.SetColumn(neighbour, 1);
        var row = new Grid
        {
            Width = 394,
            Margin = new Thickness(20),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            Children = { tile, neighbour },
        };
        var window = new Window { Width = 600, Height = 300, Content = row };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var accepted = new List<NumberChange>();
        tile.ChangeAccepted += (_, e) => accepted.Add(e.Change);
        return new Sample(window, tile, neighbour, accepted);
    }

    private static T Part<T>(Control tile, string name) where T : Control =>
        tile.GetVisualDescendants().OfType<T>().First(part => part.Name == name);

    private static Rect InWindow(Visual visual, Window window) =>
        new(visual.TranslatePoint(default, window)!.Value, visual.Bounds.Size);

    private static List<Rect> Layout(Sample sample) =>
    [
        InWindow(sample.Tile, sample.Window),
        InWindow(Part<TextBlock>(sample.Tile, "PART_Value"), sample.Window),
        InWindow(sample.Neighbour, sample.Window),
        InWindow(Part<SelectableTextBlock>(sample.Neighbour, "PART_Value"), sample.Window),
    ];

    private static void Run() => Dispatcher.UIThread.RunJobs();

    private static void ClickNumber(Sample sample)
    {
        var number = Part<TextBlock>(sample.Tile, "PART_Value");
        var point = number.TranslatePoint(new Point(number.Bounds.Width / 2, number.Bounds.Height / 2), sample.Window)!.Value;
        sample.Window.MouseMove(point);
        sample.Window.MouseDown(point, MouseButton.Left);
        sample.Window.MouseUp(point, MouseButton.Left);
        Run();
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Run();
    }

    [AvaloniaFact]
    public void Opening_the_field_and_typing_moves_nothing()
    {
        var sample = Show();
        var atRest = Layout(sample);

        ClickNumber(sample);
        var field = Part<TextBox>(sample.Tile, "PART_Field");
        Assert.True(field.IsFocused);
        Assert.Contains(TrackedValueTile.EditingClass, sample.Tile.Classes);
        Assert.Equal("9", field.SelectedText);
        Assert.Equal(atRest, Layout(sample));

        // The text in the field stands where the number stood.
        var presenter = field.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        Assert.Equal(atRest[1].Position, InWindow(presenter, sample.Window).Position);
        Assert.Equal(atRest[1].Height, presenter.Bounds.Height);

        sample.Window.KeyTextInput("-5");
        Run();
        Assert.Contains(TrackedValueTile.PreviewClass, sample.Tile.Classes);
        Assert.Equal(atRest, Layout(sample));

        sample.Window.KeyTextInput("abc");
        Run();
        Assert.Contains(TrackedValueTile.InvalidClass, sample.Tile.Classes);
        Assert.Equal(atRest, Layout(sample));

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void Enter_passes_on_a_valid_change_and_closes_the_field()
    {
        var sample = Show();

        ClickNumber(sample);
        sample.Window.KeyTextInput("-5");
        Run();
        Press(sample.Window, Key.Enter);

        Assert.Equal([new NumberChange(NumberChangeKind.Subtract, 5)], sample.Accepted);
        Assert.False(Part<TextBox>(sample.Tile, "PART_Field").IsFocused);
        Assert.DoesNotContain(TrackedValueTile.EditingClass, sample.Tile.Classes);

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void Enter_on_an_invalid_entry_does_nothing_and_shows_it()
    {
        var sample = Show();

        ClickNumber(sample);
        sample.Window.KeyTextInput("abc");
        Run();
        Press(sample.Window, Key.Enter);

        Assert.Empty(sample.Accepted);
        Assert.True(Part<TextBox>(sample.Tile, "PART_Field").IsFocused);
        Assert.Contains(TrackedValueTile.InvalidClass, sample.Tile.Classes);
        Assert.False(string.IsNullOrEmpty(sample.Tile.Message));

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void The_outline_and_the_hover_oval_reach_past_the_text()
    {
        var sample = Show();

        var frame = Part<Border>(sample.Tile, "PART_FieldFrame");
        var oval = Part<Border>(sample.Tile, "PART_HoverOval");
        Assert.True(frame.Bounds.X < 0, $"frame {frame.Bounds}");
        Assert.True(oval.Bounds.X < 0, $"oval {oval.Bounds}");
        Assert.True(frame.Bounds.Width > sample.Tile.Bounds.Width, $"frame {frame.Bounds} tile {sample.Tile.Bounds}");

        sample.Window.Close();
    }
}
