using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Shell.Gallery;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The gallery is too large to rebuild per opening, so the instance startup warmed must be the one
/// the shell shows, every later opening must show that same instance, and startup must leave it laid
/// out in the workspace so the first opening has no layout to do.
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
    public void Startup_lays_out_the_gallery_in_the_workspace_and_leaves_it_hidden()
    {
        var gallery = new GalleryContent();
        gallery.Adopt(GalleryContent.Create());
        var shell = BuildShell(gallery);
        var window = new Window { Width = 1280, Height = 800, Content = new AppShellView { DataContext = shell } };
        window.Show();

        for (var pass = 0; pass < 1000 && !shell.IsReady; pass++)
        {
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(shell.IsReady);
        Assert.False(shell.IsGalleryShown);
        var view = gallery.View!;
        Assert.False(view.IsEffectivelyVisible);
        Assert.True(view.IsArrangeValid);
        Assert.True(view.Bounds.Width > 0);

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

    private static AppShellViewModel BuildShell(GalleryContent gallery)
    {
        IReadOnlyList<IGameSystem> systems = [new EmptyGameSystem()];
        ICampaignRepository repository = new InMemoryCampaignRepository();
        var repositoriesBySystem = systems.ToDictionary(system => system.Id, _ => repository);
        var preparations = new CampaignPreparationCache(repositoriesBySystem, systems);
        var campaignLibrary = new CampaignLibraryViewModel(
            repositoriesBySystem, new CreateCampaign(repositoriesBySystem, TimeProvider.System), preparations, systems, _ => Task.CompletedTask);
        var layoutStore = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-gallery-tests-{Guid.NewGuid():N}"));

        return new AppShellViewModel(systems, repositoriesBySystem, campaignLibrary, preparations, layoutStore, gallery, []);
    }
}
