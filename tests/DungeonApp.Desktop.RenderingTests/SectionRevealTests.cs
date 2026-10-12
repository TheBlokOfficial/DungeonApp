using System;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// A foldable section unrolls on the window's frames: built open it shows at once, a click starts
/// the motion, and once it ends a folded section takes no space and its content is not visible.
/// </summary>
public sealed class SectionRevealTests
{
    /// <summary>Advances the window's frames until the section stops moving.</summary>
    internal static bool Settle(SectionReveal reveal)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (reveal.IsMoving)
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            Thread.Sleep(10);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Dispatcher.UIThread.RunJobs();
        return true;
    }

    [AvaloniaFact]
    public void An_expander_unrolls_its_content_and_rolls_it_up_to_nothing()
    {
        var content = new TextBlock { Text = string.Join(" ", Enumerable.Repeat("Długi tekst sekcji.", 80)), TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var expander = new Expander { Header = "Sekcja", Content = content, IsExpanded = true };
        var window = new Window { Width = 400, Height = 800, Content = new ScrollViewer { Content = expander } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var reveal = expander.GetVisualDescendants().OfType<SectionReveal>().Single();
        Assert.False(reveal.IsMoving);
        var openHeight = reveal.Bounds.Height;
        Assert.True(openHeight > 100);

        expander.IsExpanded = false;
        Dispatcher.UIThread.RunJobs();
        Assert.True(reveal.IsMoving);
        Assert.True(content.IsEffectivelyVisible);

        Assert.True(Settle(reveal));
        Assert.Equal(0, reveal.Bounds.Height);
        Assert.False(content.IsEffectivelyVisible);

        expander.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(content.IsEffectivelyVisible);
        Assert.True(reveal.Child!.IsHitTestVisible);

        // Flipped back mid-way, the motion turns from where it is.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        expander.IsExpanded = false;
        Dispatcher.UIThread.RunJobs();
        expander.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Settle(reveal));
        Assert.Equal(openHeight, reveal.Bounds.Height);
        Assert.Equal(1d, reveal.Progress);
        Assert.Equal(1d, content.GetVisualAncestors().OfType<Border>().First(b => b.Name == "ExpanderContent").Opacity);

        window.Close();
    }
}
