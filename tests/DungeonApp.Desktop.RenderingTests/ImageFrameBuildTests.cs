using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Shell.Gallery.Sections;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Entries;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Builds the image frame in a window: a theme error or wrongly typed resource crashes the control
/// only when it is created. Builds the gallery sample (all three states in both aspect ratios and a
/// monochrome icon) and takes one frame through every state, without assertions about appearance.
/// </summary>
public sealed class ImageFrameBuildTests
{
    private static Window Show(Control content)
    {
        var window = new Window { Width = 1000, Height = 800, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void The_gallery_sample_builds_every_state_in_both_proportions()
    {
        var window = Show(new ImagesSection());

        var frames = window.GetVisualDescendants().OfType<ImageFrame>().ToList();
        Assert.Equal(8, frames.Count);
        Assert.All(frames, frame => Assert.NotEmpty(frame.GetVisualChildren()));
        // Two bitmaps and one vector icon.
        Assert.Equal(3,frames.Count(frame => frame.Source is not null));

        window.Close();
    }

    /// <summary>
    /// A vector icon takes the placeholder icon's place - same icon control, same size and
    /// foreground - and the raster picture stays hidden; without it the placeholder shows the
    /// frame's own icon again.
    /// </summary>
    [AvaloniaFact]
    public void A_vector_icon_is_drawn_where_the_placeholder_icon_is()
    {
        var window = Show(new ImagesSection());
        var frame = window.GetVisualDescendants().OfType<ImageFrame>().Single(candidate => candidate.Name == "SquareVector");

        var picture = frame.GetVisualDescendants().OfType<Image>().Single(image => image.Name == "PART_Picture");
        var icon = frame.GetVisualDescendants().OfType<ForegroundIcon>().Single(candidate => candidate.Name == "PART_Icon");
        Assert.Contains(":vector", frame.Classes);
        Assert.False(picture.IsVisible);
        Assert.True(icon.IsEffectivelyVisible);
        Assert.Same(frame.Source, icon.Source);
        Assert.NotSame(frame.Icon, icon.Source);
        Assert.Same(frame.Foreground, icon.Foreground);

        frame.Source = null;
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(":vector", frame.Classes);
        Assert.Same(frame.Icon, icon.Source);

        window.Close();
    }

    [AvaloniaFact]
    public void A_frame_goes_through_placeholder_error_and_picture_and_back()
    {
        var picture = new ImagesSection().FindControl<ImageFrame>("SquarePicture")!.Source;
        Assert.NotNull(picture);

        var frame = new ImageFrame { Width = 120, Height = 120 };
        var window = Show(frame);

        Assert.DoesNotContain(":picture", frame.Classes);

        frame.Message = "Brak pliku";
        frame.Detail = "obrazy/plik.png";
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":error", frame.Classes);

        frame.Source = picture;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":picture", frame.Classes);
        Assert.DoesNotContain(":error", frame.Classes);

        frame.Source = null;
        frame.Message = null;
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(":picture", frame.Classes);
        Assert.DoesNotContain(":error", frame.Classes);

        window.Close();
    }

    private sealed class PicturedCatalog : IContentTypeCatalog
    {
        public static readonly ContentTypeReference Type = new(ContentId.Create("sys"), ContentId.Create("thing"));

        public bool HasSet(ContentId set) => set == Type.Set;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = new ContentTypeDescriptor(Type, "Thing", 1, "picture");
            return reference == Type;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = null;
            return true;
        }
    }

    /// <summary>
    /// The library's way to the frame with the real decoder: a PNG in the pack's subdirectory is read
    /// into the frame, a file that is not there puts the frame in its error state.
    /// </summary>
    [AvaloniaFact]
    public async System.Threading.Tasks.Task An_entry_picture_is_shown_in_a_frame_read_or_missing()
    {
        var packs = Path.Combine(Path.GetTempPath(), $"dungeonapp-image-frame-{System.Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(packs, "cnt", "entries"));
            Directory.CreateDirectory(Path.Combine(packs, "cnt", "obrazy"));
            File.WriteAllText(Path.Combine(packs, "cnt", "pack.json"),
                """{ "formatVersion": 1, "id": "cnt", "name": "Pack", "version": { "major": 1, "minor": 0 } }""");
            File.WriteAllText(Path.Combine(packs, "cnt", "entries", "a.json"),
                """{ "id": "a", "name": "A", "template": "sys:thing", "templateVersion": 1, "values": { "picture": "obrazy/a.png" } }""");
            File.WriteAllText(Path.Combine(packs, "cnt", "entries", "b.json"),
                """{ "id": "b", "name": "B", "template": "sys:thing", "templateVersion": 1, "values": { "picture": "obrazy/brak.png" } }""");

            using (var asset = AssetLoader.Open(new System.Uri("avares://DungeonApp.Desktop/Assets/Gallery/sample.png")))
            using (var file = File.Create(Path.Combine(packs, "cnt", "obrazy", "a.png")))
            {
                asset.CopyTo(file);
            }

            var registry = await new ContentPackLoader(packs, new PicturedCatalog()).LoadAsync();
            var read = EntryPicture.Load(registry, registry.Entries.Single(entry => entry.Address.Entry.Value == "a"));
            var missing = EntryPicture.Load(registry, registry.Entries.Single(entry => entry.Address.Entry.Value == "b"));

            var frame = new ImageFrame { Width = 150, Height = 200 };
            var window = Show(frame);

            read.ShowIn(frame);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(":picture", frame.Classes);

            missing.ShowIn(frame);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(":error", frame.Classes);
            Assert.Equal("obrazy/brak.png", frame.Detail);

            window.Close();
            (read.Source as System.IDisposable)?.Dispose();
        }
        finally
        {
            Directory.Delete(packs, recursive: true);
        }
    }
}
