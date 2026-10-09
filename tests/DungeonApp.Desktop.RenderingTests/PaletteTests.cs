using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The palette builds in a window and answers filtering, keys and a call from outside the window.</summary>
public sealed class PaletteTests
{
    private static readonly PaletteItem[] Items =
    [
        new("a", "DungeonIconSearch", "Skrzynia"),
        new("b", "DungeonIconSearch", "Strażnik", "humanoid", "1/8"),
        new("c", "DungeonIconSearch", "Strażnik bramy"),
    ];

    [AvaloniaFact]
    public void Typing_filters_and_a_number_sets_the_quantity_shown_on_rows()
    {
        using var fixture = new Fixture();
        var chosen = new List<(string Name, int Quantity)>();
        var task = fixture.Open(chosen, allowKeepOpen: false);

        Assert.Equal(3, fixture.Palette.Rows.Count);
        Assert.True(fixture.Palette.Rows[0].IsSelected);

        fixture.Type("2 straż");
        Assert.Equal(2, fixture.Palette.Rows.Count);
        Assert.Equal("Strażnik ×2", fixture.Palette.Rows[0].DisplayName);

        fixture.Type("nic");
        Assert.Empty(fixture.Palette.Rows);
        Assert.False(task.IsCompleted);
    }

    [AvaloniaFact]
    public void Arrows_move_selection_and_enter_chooses_and_closes()
    {
        using var fixture = new Fixture();
        var chosen = new List<(string Name, int Quantity)>();
        var task = fixture.Open(chosen, allowKeepOpen: false);

        fixture.Press(Key.Down);
        fixture.Press(Key.Down);
        fixture.Press(Key.Down);
        Assert.Equal(2, fixture.Palette.SelectedIndex);
        fixture.Press(Key.Up);
        Assert.Equal(1, fixture.Palette.SelectedIndex);

        fixture.Press(Key.Enter);
        Assert.Equal([("Strażnik", 1)], chosen);
        Assert.True(task.IsCompleted);
        Assert.DoesNotContain(fixture.Overlay.Children, c => c is Palette);
    }

    [AvaloniaFact]
    public void Ctrl_enter_chooses_and_keeps_the_palette_open_with_an_empty_field()
    {
        using var fixture = new Fixture();
        var chosen = new List<(string Name, int Quantity)>();
        var task = fixture.Open(chosen, allowKeepOpen: true);

        fixture.Type("3 skrz");
        fixture.Press(Key.Enter, RawInputModifiers.Control);

        Assert.Equal([("Skrzynia", 3)], chosen);
        Assert.False(task.IsCompleted);
        Assert.Contains(fixture.Overlay.Children, c => c is Palette);
        Assert.Equal(3, fixture.Palette.Rows.Count);
    }

    [AvaloniaFact]
    public void Escape_closes_and_a_second_palette_replaces_the_first()
    {
        using var fixture = new Fixture();
        var first = fixture.Open([], allowKeepOpen: false);
        var second = fixture.Open([], allowKeepOpen: false);

        Assert.True(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.Single(fixture.Overlay.Children.OfType<Palette>());

        fixture.Press(Key.Escape);
        Assert.True(second.IsCompleted);
        Assert.Empty(fixture.Overlay.Children.OfType<Palette>());
    }

    [AvaloniaFact]
    public void An_origin_outside_any_window_finds_the_overlay_of_the_main_window()
    {
        using var fixture = new Fixture();
        var detached = new Button();
        var previous = Palette.MainWindowProvider;
        Palette.MainWindowProvider = () => fixture.Window;
        try
        {
            Assert.Same(fixture.Overlay, Palette.FindOverlay(detached));
        }
        finally
        {
            Palette.MainWindowProvider = previous;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly Button origin = new() { Content = "Otwórz" };

        public Fixture()
        {
            Overlay = new WindowOverlay();
            Window = new Window { Width = 800, Height = 600, Content = new Panel { Children = { origin, Overlay } } };
            Window.Show();
            Dispatcher.UIThread.RunJobs();
        }

        public Window Window { get; }

        public WindowOverlay Overlay { get; }

        public Palette Palette => Overlay.Children.OfType<Palette>().Single();

        public Task Open(List<(string Name, int Quantity)> chosen, bool allowKeepOpen)
        {
            var task = Palette.ShowAsync(origin, new PaletteOptions
            {
                TargetText = "Dodaj do: Jaskinia",
                Placeholder = "Szukaj…",
                AllowQuantity = true,
                AllowKeepOpen = allowKeepOpen,
                Search = query => Items
                    .Where(i => i.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                    .ToList(),
                Choose = (item, quantity) =>
                {
                    chosen.Add((item.Name, quantity));
                    return Task.CompletedTask;
                },
            });
            Dispatcher.UIThread.RunJobs();
            return task;
        }

        public void Type(string text)
        {
            Window.KeyTextInput(text);
            Dispatcher.UIThread.RunJobs();
        }

        public void Press(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            Window.KeyPress(key, modifiers, PhysicalKey.None, null);
            Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => Window.Close();
    }
}
