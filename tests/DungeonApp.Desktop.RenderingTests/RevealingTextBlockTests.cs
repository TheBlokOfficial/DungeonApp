using System;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The trimmed text that shows itself whole in place: built in a window, driven by the pointer. The
/// full line opens only over trimmed text, exactly where the text stands, stays open while the
/// pointer moves over the text (the overlay takes no pointer input, so nothing flickers) and closes
/// when the pointer leaves or presses. It adds only what follows the cut, on the opaque card surface:
/// the letters before the cut stay the trimmed text's own. Its look follows the text's, however the
/// text got it. A name is cut at any letter, the ellipsis straight against it.
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

    // The fades run on the clock; the full line opens and closes at once with animations off.
    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            Thread.Sleep(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        return true;
    }

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

        // Moving over the text keeps it open: the overlay does not take the pointer from the text.
        window.MouseMove(Over(text, window) + new Point(20, 0));
        Dispatcher.UIThread.RunJobs();
        Assert.True(text.IsRevealed);

        window.MouseMove(new Point(790, 290));
        Dispatcher.UIThread.RunJobs();
        Assert.False(text.IsRevealed);
        Assert.True(WaitUntil(() => FullLine(window) is null));

        window.Close();
    }

    // The opaque surface the full line's added part stands on.
    private static Border Surface(TextBlock fullLine) => ((Panel)fullLine.GetVisualParent()!).Children.OfType<Border>().Single();

    [AvaloniaFact]
    public void The_full_line_stands_on_the_opaque_card_surface_not_on_what_is_behind_the_text()
    {
        var (window, text) = Show(width: 80);

        window.MouseMove(Over(text, window));
        Dispatcher.UIThread.RunJobs();

        var surface = Surface(FullLine(window)!);
        Assert.True(window.TryFindResource("DungeonBackstageCardBrush", window.ActualThemeVariant, out var card));
        Assert.Same(card, surface.Background);
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background);
        Assert.Equal(255, brush.Color.A);
        Assert.Equal(1d, brush.Opacity);

        window.Close();
    }

    [AvaloniaFact]
    public void The_full_line_adds_only_what_follows_the_cut_and_the_letters_before_it_stay_the_texts_own()
    {
        var (window, text) = Show(width: 80);
        var bounds = text.Bounds;
        var textLeft = text.TranslatePoint(default, window)!.Value.X;

        window.MouseMove(Over(text, window));
        Dispatcher.UIThread.RunJobs();
        var full = FullLine(window)!;
        var surface = Surface(full);
        Assert.True(WaitUntil(() => full.Opacity == 1d && surface.Opacity == 1d));

        // The cut: after the letters kept, before the ellipsis - inside the text, not at its edges.
        var cut = surface.TranslatePoint(default, window)!.Value.X;
        Assert.InRange(cut, textLeft + 1, textLeft + bounds.Width - 1);
        var clip = Assert.IsType<RectangleGeometry>(full.Clip).Rect;
        Assert.Equal(cut, full.TranslatePoint(new Point(clip.X, 0), window)!.Value.X, precision: 6);
        Assert.Equal(bounds, text.Bounds);

        window.MouseMove(new Point(790, 290));
        Dispatcher.UIThread.RunJobs();
        Assert.False(text.IsRevealed);

        // It fades out from fully open, not stands still until it is cut off.
        Assert.True(WaitUntil(() => full.Opacity < 0.5 && surface.Opacity < 0.5));
        Assert.True(WaitUntil(() => FullLine(window) is null));
        Assert.Equal(bounds, text.Bounds);

        window.Close();
    }

    [AvaloniaFact]
    public void The_full_line_takes_every_property_of_the_texts_look_however_the_text_got_it()
    {
        var (window, text) = Show(width: 80);
        window.Styles.Add(new Style(selector => selector.OfType<SelectableTextBlock>().Class("styled"))
        {
            Setters =
            {
                new Setter(TextBlock.ForegroundProperty, Brushes.Orange),
                new Setter(TextBlock.FontWeightProperty, FontWeight.SemiBold),
                new Setter(TextBlock.FontStyleProperty, FontStyle.Italic),
                new Setter(TextBlock.FontSizeProperty, 21d),
                new Setter(TextBlock.LetterSpacingProperty, 0.4),
                new Setter(TextBlock.LineHeightProperty, 30d),
                new Setter(TextBlock.FontFeaturesProperty, new FontFeatureCollection { FontFeature.Parse("tnum") }),
            },
        });
        text.Classes.Add("styled");
        text.FontStretch = FontStretch.Condensed;
        text.Padding = new Thickness(2, 1, 0, 0);
        Dispatcher.UIThread.RunJobs();
        Assert.True(text.IsTrimmed);

        window.MouseMove(Over(text, window));
        Dispatcher.UIThread.RunJobs();
        var full = FullLine(window)!;

        Assert.Same(Brushes.Orange, full.Foreground);
        Assert.Equal(text.FontFamily, full.FontFamily);
        Assert.Equal(text.FontSize, full.FontSize);
        Assert.Equal(text.FontWeight, full.FontWeight);
        Assert.Equal(text.FontStyle, full.FontStyle);
        Assert.Equal(text.FontStretch, full.FontStretch);
        Assert.Same(text.FontFeatures, full.FontFeatures);
        Assert.Equal(text.LetterSpacing, full.LetterSpacing);
        Assert.Equal(text.LineHeight, full.LineHeight);
        Assert.Equal(text.LineSpacing, full.LineSpacing);
        Assert.Equal(text.BaselineOffset, full.BaselineOffset);
        Assert.Equal(text.TextDecorations, full.TextDecorations);
        Assert.Equal(text.Padding, full.Padding);
        Assert.Equal(text.FlowDirection, full.FlowDirection);

        // A change while it is open reaches it too.
        text.Foreground = Brushes.Teal;
        Assert.Same(Brushes.Teal, full.Foreground);

        window.Close();
    }

    [AvaloniaFact]
    public void A_name_is_cut_at_any_letter_with_the_ellipsis_straight_against_the_last_one_kept()
    {
        var (window, text) = Show(width: 80);
        var stockLeavesASpace = false;

        for (var width = 40; width < 400; width++)
        {
            text.TextTrimming = TextTrimming.CharacterEllipsis;
            text.Width = width;
            Dispatcher.UIThread.RunJobs();
            stockLeavesASpace |= Shown(text).EndsWith(" …", StringComparison.Ordinal);

            text.TextTrimming = TightCharacterEllipsis.Instance;
            Dispatcher.UIThread.RunJobs();
            if (!text.IsTrimmed)
            {
                Assert.Equal(LongText, Shown(text));
                continue;
            }

            var shown = Shown(text);
            Assert.EndsWith("…", shown, StringComparison.Ordinal);
            var kept = shown[..^1];
            Assert.StartsWith(kept, LongText, StringComparison.Ordinal);
            Assert.Equal(kept.TrimEnd(), kept);
        }

        // The sweep crosses a cut right after a space, where the stock ellipsis keeps it.
        Assert.True(stockLeavesASpace);

        window.Close();
    }

    private static string Shown(RevealingTextBlock text) =>
        string.Concat(text.TextLayout.TextLines.SelectMany(line => line.TextRuns).Select(run => run.Text.ToString()));

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
        Assert.Null(FullLine(window));
        window.MouseUp(over, MouseButton.Left);

        window.Close();
    }
}
