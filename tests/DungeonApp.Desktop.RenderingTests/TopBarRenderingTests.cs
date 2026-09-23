using System;
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
/// that. The button's own style never set Height, but the global <c>Style Selector="Button"</c> in
/// BuiltInControls.axaml does (38, DungeonControlHeight) - a more specific selector that leaves a
/// property alone does not un-set a less specific selector's Setter for it, so that fixed 38 silently
/// beat <c>VerticalAlignment="Stretch"</c> and the button never grew to fill the bar. Every assertion
/// below measures the button against the bar's own Border, not against a hard-coded pixel count, so it
/// keeps catching the same class of mistake even if the tokens change.
/// </summary>
public sealed class TopBarRenderingTests
{
    private const double BarBottomEdge = 1;

    [AvaloniaFact]
    public void The_change_system_buttons_height_fills_the_bars_content_area()
    {
        var window = BuildWindow();
        var bar = FindTopBarBorder(window);
        var button = FindChangeSystemButton(window);

        var expectedHeight = bar.Bounds.Height - BarBottomEdge;

        Assert.Equal(expectedHeight, button.Bounds.Height);
    }

    [AvaloniaFact]
    public void The_change_system_buttons_top_edge_meets_the_bars_top_edge()
    {
        var window = BuildWindow();
        var bar = FindTopBarBorder(window);
        var button = FindChangeSystemButton(window);

        var barTop = TopLeftIn(bar, window).Y;
        var buttonTop = TopLeftIn(button, window).Y;

        Assert.Equal(barTop, buttonTop);
    }

    [AvaloniaFact]
    public void The_change_system_button_is_as_wide_as_the_bar_is_tall()
    {
        var window = BuildWindow();
        var bar = FindTopBarBorder(window);
        var button = FindChangeSystemButton(window);

        Assert.Equal(bar.Bounds.Height, button.Bounds.Width);
    }

    [AvaloniaFact]
    public void The_change_system_buttons_right_edge_meets_the_bars_right_edge()
    {
        var window = BuildWindow();
        var bar = FindTopBarBorder(window);
        var button = FindChangeSystemButton(window);

        var barRight = TopLeftIn(bar, window).X + bar.Bounds.Width;
        var buttonRight = TopLeftIn(button, window).X + button.Bounds.Width;

        Assert.Equal(barRight, buttonRight);
    }

    [AvaloniaFact]
    public void The_templates_left_line_element_spans_the_buttons_full_height()
    {
        var window = BuildWindow();
        var button = FindChangeSystemButton(window);
        var leftLineBorder = FindTemplateLeftLineBorder(button);

        Assert.Equal(button.Bounds.Height, leftLineBorder.Bounds.Height);
    }

    private static Point TopLeftIn(Visual visual, Visual ancestor) =>
        visual.TranslatePoint(new Point(0, 0), ancestor)!.Value;

    private static Button FindChangeSystemButton(Window window) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => b.Classes.Contains("frame-action"));

    private static Border FindTopBarBorder(Window window)
    {
        var button = FindChangeSystemButton(window);

        return window.GetVisualDescendants()
            .OfType<Border>()
            .First(b => b.GetVisualDescendants().Contains(button));
    }

    private static Border FindTemplateLeftLineBorder(Button button) =>
        button.GetVisualDescendants().OfType<Border>().First(b => b.BorderThickness.Left > 0);

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
