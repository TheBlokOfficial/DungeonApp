using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The one-line tag row: tags that fit stand, the next takes the remainder, the rest are not shown.</summary>
public sealed class OneLineTagPanelTests
{
    private static Border Tag(double width) => new() { Width = width, Height = 20, MinWidth = 0 };

    private static (Window Window, OneLineTagPanel Panel) Show(double width, params Control[] tags)
    {
        var panel = new OneLineTagPanel
        {
            ItemSpacing = 6,
            Width = width,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };
        panel.Children.AddRange(tags);
        var window = new Window { Width = 800, Height = 400, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, panel);
    }

    [AvaloniaFact]
    public void Tags_that_fit_stand_in_one_line_at_their_own_width()
    {
        var (window, panel) = Show(300, Tag(50), Tag(80));

        Assert.Equal(0, panel.Children[0].Bounds.X);
        Assert.Equal(56, panel.Children[1].Bounds.X);
        Assert.Equal(20, panel.Bounds.Height);

        window.Close();
    }

    [AvaloniaFact]
    public void A_tag_that_does_not_fit_takes_the_remainder_and_the_ones_after_it_are_not_shown()
    {
        var text = new RevealingTextBlock { Text = "dowolny miecz zadający obrażenia cięte", TextWrapping = Avalonia.Media.TextWrapping.NoWrap, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        var (window, panel) = Show(200, Tag(50), text, Tag(40));

        Assert.Equal(56, panel.Children[1].Bounds.X);
        // The tag is cut to the remainder (144) and its text keeps to one line.
        Assert.InRange(panel.Children[1].Bounds.Width, 100, 144);
        Assert.True(panel.Children[2].Bounds.X >= panel.Bounds.Width);
        Assert.Single(((RevealingTextBlock)panel.Children[1]).TextLayout.TextLines);

        window.Close();
    }

    [AvaloniaFact]
    public void A_remainder_too_narrow_for_a_tag_shows_nothing()
    {
        var (window, panel) = Show(100, Tag(80), Tag(60));

        Assert.True(panel.Children[1].Bounds.X >= panel.Bounds.Width);

        window.Close();
    }
}
