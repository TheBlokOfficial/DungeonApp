using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DungeonApp.Desktop.Shell.Gallery;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The gallery is too large to rebuild per opening, so the instance startup warmed must be the one
/// the shell shows, and every later opening must show that same instance.
/// </summary>
public sealed class GalleryContentTests
{
    [AvaloniaFact]
    public void The_warmed_view_is_the_one_shown_and_stays_the_gallery()
    {
        var gallery = new GalleryContent();
        var warmed = GalleryContent.Create();
        var window = new Window { Width = 1280, Height = 900, Content = warmed };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Content = null;

        gallery.Adopt(warmed);
        gallery.Adopt(GalleryContent.Create());

        Assert.Same(warmed, gallery.View);
        Assert.Same(warmed, gallery.EnsureView());

        window.Content = gallery.View;
        Dispatcher.UIThread.RunJobs();
        Assert.True(warmed.IsEffectivelyVisible);

        window.Close();
    }

    [AvaloniaFact]
    public void Without_a_warmed_view_the_first_opening_builds_one_and_keeps_it()
    {
        var gallery = new GalleryContent();

        var first = gallery.EnsureView();

        Assert.IsType<GalleryView>(first);
        Assert.Same(first, gallery.EnsureView());
    }
}
