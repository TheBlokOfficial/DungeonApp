using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Workspace.Leaf;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Builds the desk's command strip in a window: a wrongly typed XAML resource compiles but crashes the
/// view when it is created.
/// </summary>
public sealed class DeskLeafViewBuildTests
{
    [AvaloniaFact]
    public void The_leaf_builds_with_one_enabled_button_that_closes_the_campaign()
    {
        var closed = 0;
        var view = new DeskLeafView
        {
            DataContext = new DeskLeafViewModel(() =>
            {
                closed++;
                return Task.CompletedTask;
            }),
        };
        var window = new Window { Width = 400, Height = 200, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(4, buttons.Count);
        Assert.Equal(3, buttons.Count(button => !button.IsEffectivelyEnabled));

        var close = buttons.Single(button => button.IsEffectivelyEnabled);
        close.Command!.Execute(null);
        Assert.Equal(1, closed);
        window.Close();
    }
}
