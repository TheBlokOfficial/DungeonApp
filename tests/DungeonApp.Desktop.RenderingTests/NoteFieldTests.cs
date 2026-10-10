using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The GM's note has two forms - text to read and a field - in one place: a click turns it into the
/// field without moving the note, its text or anything around it, and leaving the field hands the
/// text over once.
/// </summary>
public sealed class NoteFieldTests
{
    public static TheoryData<string> Notes => new()
    {
        "",
        "Ukradł klucz do skarbca — trzyma go w lewej sakwie.\nUcieknie, gdy zostanie sam.",
    };

    [AvaloniaTheory]
    [MemberData(nameof(Notes))]
    public void Clicking_the_note_turns_it_into_a_field_without_moving_anything(string text)
    {
        var (window, note, below, _) = Show(text);
        var before = Layout(window, note, below);

        Click(window, note);

        Assert.True(note.IsFocused);
        Assert.Equal(note.SelectionStart, note.SelectionEnd);
        Assert.Equal(before, Layout(window, note, below));

        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_the_field_commits_the_text_once()
    {
        var (window, note, _, button) = Show("Ucieknie.");
        var committed = new List<string>();
        note.Committed += (_, e) => committed.Add(e.Text);

        Click(window, note);
        note.CaretIndex = note.Text!.Length;
        window.KeyTextInput(" Szybko.");
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(committed);

        button.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Ucieknie. Szybko."], committed);

        window.Close();
    }

    private static (Window Window, NoteField Note, TextBlock Below, Button Button) Show(string text)
    {
        var note = new NoteField { Text = text };
        var below = new TextBlock { Text = "Pod notatką" };
        var button = new Button { Content = "Dalej" };
        var window = new Window
        {
            Width = 640,
            Height = 400,
            Content = new StackPanel { Width = 560, Margin = new Thickness(20), Children = { note, below, button } },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, note, below, button);
    }

    private static void Click(Window window, NoteField note)
    {
        var point = note.TranslatePoint(new Point(note.Bounds.Width / 2, note.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    // Where the note, its text (and placeholder) and its neighbour stand in the window.
    private static string Layout(Window window, NoteField note, Control below)
    {
        var text = note.GetVisualDescendants().OfType<TextPresenter>().Single();
        var placeholder = note.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "PART_Placeholder");
        return string.Join(" | ", new Control[] { note, text, placeholder, below }.Select(control => InWindow(window, control)));
    }

    private static Rect InWindow(Window window, Control control) =>
        new(control.TranslatePoint(default, window)!.Value, control.Bounds.Size);
}
