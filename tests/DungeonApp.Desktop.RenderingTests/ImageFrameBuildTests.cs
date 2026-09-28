using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Shell.Gallery.Sections;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Ramka obrazka budowana w oknie: błąd w motywie albo zasób złego typu wywraca kontrolkę dopiero
/// przy utworzeniu. Stawia próbkę galerii (wszystkie trzy stany w obu proporcjach) i przeprowadza
/// jedną ramkę przez każdy stan - bez asercji o wyglądzie.
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
        Assert.Equal(7, frames.Count);
        Assert.All(frames, frame => Assert.NotEmpty(frame.GetVisualChildren()));
        Assert.Equal(2, frames.Count(frame => frame.Source is not null));

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
}
