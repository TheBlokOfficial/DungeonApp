using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>A card's running text built in a window: one block per hard-break line, a small gap between them.</summary>
public sealed class ProseTextTests
{
    private static (Window Window, ProseText Prose) Show(string text)
    {
        var prose = new ProseText
        {
            Width = 300,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Text = text,
        };
        var window = new Window { Width = 800, Height = 800, Content = prose };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, prose);
    }

    [AvaloniaFact]
    public void A_text_without_a_hard_break_is_one_block()
    {
        var (window, prose) = Show("Jedna **linia** tekstu.");

        var block = Assert.Single(prose.Blocks);
        Assert.Equal("Jedna linia tekstu.", block.Inlines!.Text);

        window.Close();
    }

    [AvaloniaFact]
    public void A_hard_break_starts_the_next_line_a_little_lower_than_a_wrapped_one()
    {
        var (window, prose) = Show("Pierwsza.\nDruga.");

        var blocks = prose.Blocks;
        Assert.Equal(["Pierwsza.", "Druga."], blocks.Select(block => block.Inlines!.Text));
        var gap = blocks[1].Bounds.Y - blocks[0].Bounds.Bottom;
        Assert.True(gap > 0);
        Assert.True(gap < blocks[0].LineHeight / 2);

        window.Close();
    }

    [AvaloniaFact]
    public void Emphasis_spanning_a_break_stays_emphasized_on_both_sides()
    {
        var (window, prose) = Show("a **b\nc** d");

        var blocks = prose.Blocks;
        Assert.Equal(2, blocks.Count);
        Assert.Contains("emphasis", blocks[0].Inlines!.OfType<Avalonia.Controls.Documents.Run>().Last().Classes);
        Assert.Contains("emphasis", blocks[1].Inlines!.OfType<Avalonia.Controls.Documents.Run>().First().Classes);

        window.Close();
    }
}
