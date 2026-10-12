using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The startup curtain's bar: it moves with every step, in equal shares, from the very first one,
/// and the curtain lifts only once the bar's slide to full has had its time - with motion on.
/// </summary>
public sealed class StartupProgressTests
{
    /// <summary>
    /// A bar whose fill never had a width must still slide from nothing on its first changes: a
    /// transition starting from an unset (NaN) width draws no fill at all until it ends, and a run
    /// of quick changes keeps restarting from that NaN.
    /// </summary>
    [AvaloniaFact]
    public void A_fresh_bar_draws_its_fill_from_the_first_quick_changes()
    {
        var bar = new ProgressBar { Width = 192, Minimum = 0, Maximum = 6, Value = 0 };
        var window = new Window { Width = 400, Height = 200, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var indicator = bar.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Indicator");

        var previous = 0d;
        for (var step = 1; step <= 3; step++)
        {
            bar.Value = step;
            Dispatcher.UIThread.RunJobs();
            for (var frame = 0; frame < 5; frame++)
            {
                Thread.Sleep(20);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
            }

            Assert.False(double.IsNaN(indicator.Width), $"Wypełnienie bez szerokości po kroku {step}.");
            Assert.True(indicator.Bounds.Width > previous, $"Wypełnienie nie urosło po kroku {step}: {indicator.Bounds.Width}.");
            previous = indicator.Bounds.Width;
        }

        window.Close();
    }

    [AvaloniaFact]
    public async Task Every_step_moves_the_bar_by_an_equal_share()
    {
        var shell = StartupFailureTests.BuildShell(new QuickStep(), new QuickStep(), new QuickStep());
        var seen = new List<int>();
        shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppShellViewModel.CompletedSteps))
            {
                seen.Add(shell.CompletedSteps);
            }
        };

        await shell.RunStartupAsync(new StartupUiContext(new ContentControl()));

        Assert.Equal(3, shell.TotalSteps);
        Assert.Equal([1, 2, 3], seen);
    }

    [AvaloniaFact]
    public async Task With_motion_the_curtain_waits_for_the_bar_to_reach_its_end()
    {
        var settle = TimeSpan.FromMilliseconds(400);
        var step = new QuickStep();
        var shell = StartupFailureTests.BuildShell(step);

        await shell.RunStartupAsync(new StartupUiContext(new ContentControl()) { ProgressSettleTime = settle });

        Assert.True(shell.IsReady);
        Assert.True(step.Finished.Elapsed >= settle - TimeSpan.FromMilliseconds(20),
            $"Kurtyna zeszła {step.Finished.ElapsedMilliseconds} ms po ostatnim kroku, przed dojściem paska.");
    }

    [AvaloniaFact]
    public async Task Without_motion_the_curtain_does_not_wait()
    {
        var step = new QuickStep();
        var shell = StartupFailureTests.BuildShell(step);

        await shell.RunStartupAsync(new StartupUiContext(new ContentControl()));

        Assert.True(shell.IsReady);
        Assert.True(step.Finished.Elapsed < TimeSpan.FromMilliseconds(400),
            $"Kurtyna czekała {step.Finished.ElapsedMilliseconds} ms mimo wyłączonych animacji.");
    }

    private sealed class QuickStep : IStartupStep
    {
        public Stopwatch Finished { get; } = new();

        public string Describe() => "Krok testowy";

        public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken)
        {
            Finished.Restart();
            return Task.CompletedTask;
        }
    }
}
