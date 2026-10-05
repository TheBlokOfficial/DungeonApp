using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The paragraph opened by an initial, built in a window: text is measured without a screen, so where
/// the text is cut and where each block stands can be checked; how the icon looks cannot.
/// </summary>
public sealed class InitialProseBuildTests
{
    private const string LongText =
        "Pierwsze dwie linie tego akapitu zaczynają się za ikoną, a kolejne wracają do lewej krawędzi. "
        + "Tekst ma **wyróżnienie** i płynie dalej tym samym rytmem linii przez kilka kolejnych zdań, "
        + "żeby na pewno zajął więcej niż dwie linie przy tej szerokości i dał się podzielić na dwa bloki.";

    private static (Window Window, InitialProse Prose) Show(string text, double width = 300)
    {
        var prose = new InitialProse
        {
            Width = width,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Icon = (DrawingImage)Avalonia.Application.Current!.FindResource("DungeonIconScrollText")!,
            Text = text,
        };
        var window = new Window { Width = 800, Height = 800, Content = prose };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, prose);
    }

    private static (ForegroundIcon Icon, SelectableTextBlock Lead, SelectableTextBlock Under) Parts(InitialProse prose)
    {
        var children = prose.GetVisualChildren().ToList();
        var blocks = children.OfType<SelectableTextBlock>().ToList();
        return (children.OfType<ForegroundIcon>().Single(), blocks[0], blocks[1]);
    }

    [AvaloniaFact]
    public void A_long_text_keeps_two_lines_beside_the_icon_and_flows_on_under_it()
    {
        var (window, prose) = Show(LongText);
        var (icon, lead, rest) = Parts(prose);

        Assert.Same(prose.FindResource("DungeonTextMutedBrush"), icon.Foreground);
        Assert.Equal(icon.Bounds.Width, icon.Bounds.Height);
        // Layout rounding puts sizes on whole pixels: two lines of 20.15 make a 41-pixel square.
        Assert.Equal(2 * lead.LineHeight, icon.Bounds.Height, tolerance: 1);

        Assert.True(lead.TextLayout.TextLines.Count <= 2);
        Assert.True(lead.Bounds.X > icon.Bounds.Right);
        Assert.Equal(0, lead.Bounds.Y);

        Assert.True(rest.IsVisible);
        Assert.Equal(0, rest.Bounds.X);
        Assert.Equal(icon.Bounds.Bottom, rest.Bounds.Y, 3);
        Assert.Equal(prose.Bounds.Width, rest.Bounds.Width);

        // The cut drops only the whitespace at it: nothing is lost or doubled.
        var shown = lead.Inlines!.Text + " " + rest.Inlines!.Text;
        Assert.Equal(LongText.Replace("**", string.Empty, System.StringComparison.Ordinal), shown);

        window.Close();
    }

    [AvaloniaFact]
    public void A_narrower_width_cuts_the_text_again()
    {
        var (window, prose) = Show(LongText, width: 500);
        var before = Parts(prose).Lead.Inlines!.Text;

        prose.Width = 240;
        Dispatcher.UIThread.RunJobs();

        var (_, lead, rest) = Parts(prose);
        Assert.NotEqual(before, lead.Inlines!.Text);
        Assert.True(lead.TextLayout.TextLines.Count <= 2);
        Assert.True(rest.IsVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void A_hard_line_break_counts_as_a_line_end()
    {
        var (window, prose) = Show("Pierwsza.\nDruga.\nTrzecia.");
        var (_, lead, rest) = Parts(prose);

        Assert.Equal("Pierwsza.\nDruga.", lead.Inlines!.Text);
        Assert.Equal("Trzecia.", rest.Inlines!.Text);

        window.Close();
    }

    [AvaloniaFact]
    public void A_short_text_stays_beside_the_icon_in_one_block()
    {
        var (window, prose) = Show("Krótki akapit.");
        var (icon, lead, rest) = Parts(prose);

        Assert.False(rest.IsVisible);
        Assert.Equal("Krótki akapit.", lead.Inlines!.Text);
        Assert.True(prose.Bounds.Height >= icon.Bounds.Height);

        window.Close();
    }
}
