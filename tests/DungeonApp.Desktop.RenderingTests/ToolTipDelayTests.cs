using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The tooltip service reads the delays from the control that carries the tip, so the theme sets them
/// on every control in the window. These tests check the values the service will read; when it opens
/// a tooltip is real time on a real pointer, which only the application shows.
/// </summary>
public sealed class ToolTipDelayTests
{
    private const int ExpectedShowDelay = 700;

    [AvaloniaFact]
    public void Every_control_waits_the_theme_delay_for_every_tooltip()
    {
        var button = new Button { Content = "A" };
        var text = new TextBlock { Text = "B" };
        var border = new Border { Child = text };
        var panel = new StackPanel { Children = { button, border } };
        var window = new Window { Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        foreach (var control in new Control[] { button, text, border, panel })
        {
            Assert.Equal(ExpectedShowDelay, ToolTip.GetShowDelay(control));
            Assert.True(ToolTip.GetBetweenShowDelay(control) < 0);
        }

        // Button's own template parts carry it too: a tip may sit on a part of a control.
        Assert.Equal(ExpectedShowDelay, ToolTip.GetShowDelay((Control)button.Presenter!));
        window.Close();
    }
}
