using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Shell.TopBar;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Headless-rendering coverage for the "Zmień system" button's own layout, fixed after the coordinator
/// traced the actual cause of the hover-covers-the-line bug: TopBarView.axaml's outer frame Border is
/// <c>DungeonTopBarHeight</c> tall with a 1px bottom edge, so its content area is one pixel shorter than
/// that. The button used to set an explicit <c>Height="{DynamicResource DungeonTopBarHeight}"</c> too -
/// one pixel taller than the space it was given - so it overflowed into the bar's own bottom line and
/// painted over it on hover. The fix drops the explicit Height in favour of
/// <c>VerticalAlignment="Stretch"</c>, so the button's own Bounds can never exceed the bar's content
/// area in the first place.
/// </summary>
public sealed class TopBarRenderingTests
{
    private const double TopBarHeight = 52;
    private const double BarBottomEdge = 1;

    [AvaloniaFact]
    public void The_change_system_buttons_bottom_edge_never_reaches_the_bars_own_bottom_line()
    {
        var window = BuildWindow();
        var button = FindChangeSystemButton(window);

        Visual buttonAsVisual = button;
        var buttonBottom = buttonAsVisual.TranslatePoint(new Point(0, button.Bounds.Height), window)!.Value.Y;
        var barContentBottom = TopBarHeight - BarBottomEdge;

        Assert.True(
            buttonBottom <= barContentBottom,
            $"Dolna krawędź przycisku ({buttonBottom}) wychodzi poza wnętrze paska ({barContentBottom}) i zasłoniłaby jego dolną linię.");
    }

    [AvaloniaFact]
    public void The_change_system_button_is_as_wide_as_the_bar_is_tall()
    {
        var window = BuildWindow();
        var button = FindChangeSystemButton(window);

        Assert.Equal(TopBarHeight, button.Bounds.Width);
    }

    private static Button FindChangeSystemButton(Window window) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Classes.Contains("frame-action"));

    private static Window BuildWindow()
    {
        var viewModel = new TopBarViewModel(new AsyncCommand(() => Task.CompletedTask))
        {
            ActiveSystemName = "Testowy system",
        };

        var view = new TopBarView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 1056, Height = 52 };
        window.Show();
        window.GetLayoutManager()!.ExecuteLayoutPass();

        return window;
    }
}
