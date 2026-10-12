using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

public sealed class ToolTipDelayTests
{
    // The library shows the next tooltip at once after one closed; the theme turns that off so every
    // tooltip waits the full delay.
    [AvaloniaFact]
    public void Every_control_waits_the_full_delay_between_tooltips()
    {
        var button = new Button();
        var text = new TextBlock();
        var panel = new StackPanel { Children = { button, text } };
        var window = new Window { Content = panel };
        window.Show();

        Assert.True(ToolTip.GetBetweenShowDelay(button) < 0);
        Assert.True(ToolTip.GetBetweenShowDelay(text) < 0);
    }
}
