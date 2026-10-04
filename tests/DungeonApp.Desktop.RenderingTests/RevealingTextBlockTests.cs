using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The trimmed text that shows itself whole in place: built in a window, driven by the pointer. The
/// full line opens only over trimmed text, exactly where the text stands, stays open while the
/// pointer moves over the text (the overlay takes no pointer input, so nothing flickers) and closes
/// when the pointer leaves or presses.
/// </summary>
public sealed class RevealingTextBlockTests
{
    private const string LongText = "Różdżka magicznych pocisków i coś jeszcze";

    private static (Window Window, RevealingTextBlock Text) Show(double width)
    {
        var text = new RevealingTextBlock
        {
            Width = width,
            Text = LongText,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
        };
        var window = new Window
        {
            Width = 800,
            Height = 300,
            Content = new Border { Background = Brushes.DarkSlateGray, Padding = new Thickness(40), Child = text },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, text);
    }

    private static Point Over(RevealingTextBlock text, Window window) => text.TranslatePoint(new Point(10, text.Bounds.Height / 2), window)!.Value;

    // The full line as shown: inside the window's tree (an overlay, not a window of its own), a
    // text block other than the trimmed one.
    private static TextBlock? FullLine(Window window) =>
        window.GetVisualDescendants().OfType<TextBlock>()
            .FirstOrDefault(block => block.Text == LongText && block is not RevealingTextBlock && block.IsEffectivelyVisible);

    [AvaloniaFact]
    public void Trimmed_text_opens_whole_in_place_under_the_pointer_and_closes_when_it_leaves()
    {
        var (window, text) = Show(width: 80);
        Assert.True(text.IsTrimmed);

        window.MouseMove(Over(text, window));
        Dispatcher.UIThread.RunJobs();
        Assert.True(text.IsRevealed);

        var full = FullLine(window);
        Assert.NotNull(full);
        Assert.Equal(TextWrapping.NoWrap, full.TextWrapping);
        Assert.Equal(text.FontSize, full.FontSize);
        Assert.True(full.Bounds.Width > text.Bounds.Width);
        var textOrigin = text.TranslatePoint(default, window)!.Value;
        var fullOrigin = full.TranslatePoint(default, window)!.Value;
        Assert.Equal(textOrigin, fullOrigin);
        Assert.Equal(Brushes.DarkSlateGray, ((Border)full.Parent!).Background);

        // Moving over the text keeps it open: the overlay does not take the pointer from the text.
        window.MouseMove(Over(text, window) + new Point(20, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.True(text.IsRevealed);

        window.MouseMove(new Point(790, 290));
        Dispatcher.UIThread.RunJobs();
        Assert.False(text.IsRevealed);

        window.Close();
    }

    [AvaloniaFact]
    public void Text_that_fits_opens_nothing()
    {
        var (window, text) = Show(width: 780 - 80);
        Assert.False(text.IsTrimmed);

        window.MouseMove(Over(text, window));
        Dispatcher.UIThread.RunJobs();

        Assert.False(text.IsRevealed);
        Assert.Null(FullLine(window));

        window.Close();
    }

    [AvaloniaFact]
    public void A_press_closes_the_full_line_so_selecting_works_on_the_text()
    {
        var (window, text) = Show(width: 80);
        var over = Over(text, window);

        window.MouseMove(over);
        Dispatcher.UIThread.RunJobs();
        Assert.True(text.IsRevealed);

        window.MouseDown(over, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(text.IsRevealed);
        window.MouseUp(over, MouseButton.Left);

        window.Close();
    }
}
