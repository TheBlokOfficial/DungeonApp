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
using DungeonApp.Core.State;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Editable text has two forms - text to read and a field - in one place, in both its uses: the GM's
/// note and a tracked value's number. A click turns it into the field without moving it, its text or
/// anything around it, and typing in the number moves nothing either. The control is as wide as its
/// text, never narrower than its minimum, so the hover background covers the text only.
/// </summary>
public sealed class EditableTextTests
{
    public static TheoryData<string> Notes => new()
    {
        "",
        "Boi się psów.",
        "Ukradł klucz do skarbca — trzyma go w lewej sakwie.\nUcieknie, gdy zostanie sam.",
    };

    private static void Run() => Dispatcher.UIThread.RunJobs();

    private static Rect InWindow(Window window, Visual visual) =>
        new(visual.TranslatePoint(default, window)!.Value, visual.Bounds.Size);

    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Run();
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Run();
    }

    // ---- The note -------------------------------------------------------------------------------

    private static (Window Window, EditableText Note, TextBlock Below, Button Button) ShowNote(string text)
    {
        var note = new EditableText { Text = text, MinWidth = 120, PlaceholderText = "Notatka MG — kliknij, żeby pisać" };
        var below = new TextBlock { Text = "Pod notatką" };
        var button = new Button { Content = "Dalej" };
        var window = new Window
        {
            Width = 640,
            Height = 400,
            Content = new StackPanel { Width = 560, Margin = new Thickness(20), Children = { note, below, button } },
        };
        window.Show();
        Run();
        return (window, note, below, button);
    }

    // Where the note, its text (and placeholder) and its neighbour stand in the window.
    private static string NoteLayout(Window window, EditableText note, Control below)
    {
        var text = note.GetVisualDescendants().OfType<TextPresenter>().Single();
        var placeholder = note.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Name == "PART_Placeholder");
        return string.Join(" | ", new Control[] { note, text, placeholder, below }.Select(control => InWindow(window, control)));
    }

    [AvaloniaTheory]
    [MemberData(nameof(Notes))]
    public void Clicking_the_note_turns_it_into_a_field_without_moving_anything(string text)
    {
        var (window, note, below, _) = ShowNote(text);
        var before = NoteLayout(window, note, below);

        Click(window, note);

        Assert.True(note.IsFocused);
        Assert.Equal(note.SelectionStart, note.SelectionEnd);
        Assert.Equal(before, NoteLayout(window, note, below));

        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Notes))]
    public void The_note_is_as_wide_as_its_text_but_not_narrower_than_its_minimum(string text)
    {
        var (window, note, _, _) = ShowNote(text);

        var shown = note.GetVisualDescendants().OfType<Control>()
            .Where(control => control is TextPresenter || (control is TextBlock { Name: "PART_Placeholder", IsVisible: true }))
            .Max(control => control.DesiredSize.Width);
        Assert.Equal(System.Math.Max(120, shown), note.Bounds.Width, 1);
        Assert.True(note.Bounds.Width < 560, $"note {note.Bounds}");

        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_the_note_commits_the_text_once()
    {
        var (window, note, _, button) = ShowNote("Ucieknie.");
        var committed = new List<string>();
        note.Committed += (_, e) => committed.Add(e.Text);

        Click(window, note);
        note.CaretIndex = note.Text!.Length;
        window.KeyTextInput(" Szybko.");
        Run();
        Assert.Empty(committed);

        button.Focus();
        Run();

        Assert.Equal(["Ucieknie. Szybko."], committed);

        window.Close();
    }

    // ---- The tracked value's number -------------------------------------------------------------

    private sealed record Sample(Window Window, TrackedValueTile Tile, StatTile Neighbour, List<NumberChange> Accepted)
    {
        public EditableText Field => Tile.GetVisualDescendants().OfType<EditableText>().Single();

        public TextBlock Maximum => Tile.GetVisualDescendants().OfType<TextBlock>().First(part => part.Name == "PART_Maximum");
    }

    private static Sample ShowTile()
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
        Run();

        var accepted = new List<NumberChange>();
        tile.ChangeAccepted += (_, e) => accepted.Add(e.Change);
        return new Sample(window, tile, neighbour, accepted);
    }

    private static List<Rect> TileLayout(Sample sample) =>
    [
        InWindow(sample.Window, sample.Tile),
        InWindow(sample.Window, sample.Field),
        InWindow(sample.Window, sample.Field.GetVisualDescendants().OfType<TextPresenter>().Single()),
        InWindow(sample.Window, sample.Maximum),
        InWindow(sample.Window, sample.Neighbour),
    ];

    [AvaloniaFact]
    public void Opening_the_number_moves_nothing()
    {
        var sample = ShowTile();
        var atRest = TileLayout(sample);

        Click(sample.Window, sample.Field);
        Assert.True(sample.Field.IsFocused);
        Assert.Contains(TrackedValueTile.EditingClass, sample.Tile.Classes);
        Assert.Equal("9", sample.Field.SelectedText);
        Assert.Equal(atRest, TileLayout(sample));

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void A_longer_entry_widens_the_field_and_pushes_the_maximum_until_the_field_is_left()
    {
        var sample = ShowTile();
        var atRest = TileLayout(sample);

        Click(sample.Window, sample.Field);
        sample.Window.KeyTextInput("-120");
        Run();

        // The text stays inside the field, so the caret and the selection stay inside its outline.
        Assert.Equal("-120", sample.Field.Text);
        var presenter = sample.Field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(presenter.Bounds.Width, sample.Field.Bounds.Width, 1);
        Assert.True(InWindow(sample.Window, sample.Field).Width > atRest[1].Width);
        Assert.True(InWindow(sample.Window, sample.Maximum).X > atRest[3].X);
        Assert.Equal(atRest[0], InWindow(sample.Window, sample.Tile));
        Assert.Equal(atRest[4], InWindow(sample.Window, sample.Neighbour));

        // Leaving the field (the shell's Escape and click-outside do the same) drops the entry.
        sample.Window.FocusManager!.Focus(null);
        Run();
        Assert.Equal(atRest, TileLayout(sample));

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void The_number_is_as_wide_as_one_digit_or_its_text()
    {
        var sample = ShowTile();

        var one = sample.Field.MinWidth;
        Assert.True(one > 0);
        Assert.Equal(one, sample.Field.Bounds.Width, 1);

        sample.Tile.Value = "12345";
        Run();
        var presenter = sample.Field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.True(sample.Field.Bounds.Width > one);
        Assert.Equal(presenter.DesiredSize.Width, sample.Field.Bounds.Width, 1);

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void The_number_refuses_letters_and_a_second_sign()
    {
        var sample = ShowTile();

        Click(sample.Window, sample.Field);
        sample.Window.KeyTextInput("-");
        sample.Window.KeyTextInput("a");
        sample.Window.KeyTextInput("+");
        sample.Window.KeyTextInput("5");
        Run();

        Assert.Equal("-5", sample.Field.Text);

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void A_bare_number_and_Enter_set_the_value()
    {
        var sample = ShowTile();

        Click(sample.Window, sample.Field);
        sample.Window.KeyTextInput("12");
        Run();
        Press(sample.Window, Key.Enter);

        Assert.Equal([new NumberChange(NumberChangeKind.Set, 12)], sample.Accepted);
        Assert.False(sample.Field.IsFocused);
        Assert.DoesNotContain(TrackedValueTile.EditingClass, sample.Tile.Classes);

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void Enter_passes_on_a_subtraction()
    {
        var sample = ShowTile();

        Click(sample.Window, sample.Field);
        sample.Window.KeyTextInput("-5");
        Run();
        Press(sample.Window, Key.Enter);

        Assert.Equal([new NumberChange(NumberChangeKind.Subtract, 5)], sample.Accepted);

        sample.Window.Close();
    }

    [AvaloniaFact]
    public void Enter_on_a_lone_sign_changes_nothing_and_leaves()
    {
        var sample = ShowTile();

        Click(sample.Window, sample.Field);
        sample.Window.KeyTextInput("+");
        Run();
        Press(sample.Window, Key.Enter);

        Assert.Empty(sample.Accepted);
        Assert.False(sample.Field.IsFocused);
        Assert.Equal("9", sample.Field.Text);

        sample.Window.Close();
    }
}
