using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Themes;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The gauge glides to a changed value; the first value, a reset and reduced motion stand at once.</summary>
public sealed class TrackGaugeGlideTests
{
    [AvaloniaFact]
    public void A_changed_value_is_drawn_in_between_and_then_on_the_target()
    {
        var (window, gauge) = Show(value: 10);
        Assert.Equal(10, gauge.DisplayedValue);

        gauge.Value = 2;
        Dispatcher.UIThread.RunJobs();
        Tick();
        Assert.InRange(gauge.DisplayedValue, 2.01, 9.999);

        for (var frame = 0; frame < 100 && gauge.DisplayedValue != 2; frame++)
        {
            Tick();
        }

        Assert.Equal(2, gauge.DisplayedValue);
        window.Close();
    }

    [AvaloniaFact]
    public void Standing_on_the_target_shows_another_entitys_numbers_at_once()
    {
        var (window, gauge) = Show(value: 10);

        gauge.Value = 3;
        gauge.Maximum = 5;
        gauge.StandOnTarget();

        Assert.Equal(3, gauge.DisplayedValue);
        Assert.Equal(5, gauge.DisplayedMaximum);
        window.Close();
    }

    [AvaloniaFact]
    public void Without_motion_the_gauge_stands_on_its_value_at_once()
    {
        var (window, gauge) = Show(value: 10);
        SystemMotion.IsReduced = true;
        try
        {
            gauge.Value = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, gauge.DisplayedValue);
        }
        finally
        {
            SystemMotion.IsReduced = false;
            window.Close();
        }
    }

    private static (Window Window, TrackGauge Gauge) Show(double value)
    {
        var gauge = new TrackGauge { Width = 200, Value = value, Maximum = 10 };
        var window = new Window { Width = 400, Height = 200, Content = gauge };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Tick();
        return (window, gauge);
    }

    private static void Tick()
    {
        Thread.Sleep(16);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
