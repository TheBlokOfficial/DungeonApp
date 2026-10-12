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
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Themes;
using Xunit;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The progress bar's fill glides towards its value and quick changes only move the target; the
/// startup curtain moves the bar with every step, in equal shares, and lifts once the bar shows full.
/// </summary>
public sealed class StartupProgressTests
{
    /// <summary>
    /// A new value every few frames - faster than the fill arrives - must neither stall the fill nor
    /// pull it back: it keeps growing frame by frame until it stands on the last value.
    /// </summary>
    [AvaloniaFact]
    public void Quick_changes_keep_the_fill_growing_to_the_last_value()
    {
        var (window, bar, fill) = ShowBar(value: 0);

        var displayed = new List<double> { fill.DisplayedPercentage };
        for (var step = 1; step <= 6; step++)
        {
            bar.Value = step;
            Dispatcher.UIThread.RunJobs();
            for (var frame = 0; frame < 3; frame++)
            {
                Tick();
                displayed.Add(fill.DisplayedPercentage);
            }
        }

        for (var frame = 0; frame < 200 && fill.DisplayedPercentage < 100; frame++)
        {
            Tick();
            displayed.Add(fill.DisplayedPercentage);
        }

        Assert.Equal(100, fill.DisplayedPercentage);
        for (var i = 1; i < displayed.Count; i++)
        {
            Assert.True(displayed[i] >= displayed[i - 1], $"Wypełnienie cofnęło się w klatce {i}: {displayed[i - 1]} → {displayed[i]}.");
        }

        // Every frame of the run of changes moved the fill - none stood still waiting for a restart.
        for (var i = 1; i <= 18; i++)
        {
            Assert.True(displayed[i] > displayed[i - 1], $"Wypełnienie stanęło w klatce {i}: {displayed[i]}.");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void The_first_value_is_shown_without_a_glide()
    {
        var (window, _, fill) = ShowBar(value: 3);

        Assert.Equal(50, fill.DisplayedPercentage);
        Assert.True(fill.WhenSettledAsync().IsCompleted);

        window.Close();
    }

    [AvaloniaFact]
    public void Without_motion_the_fill_stands_on_its_value_at_once()
    {
        var (window, bar, fill) = ShowBar(value: 0);
        SystemMotion.IsReduced = true;
        try
        {
            bar.Value = 3;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(50, fill.DisplayedPercentage);
            Assert.True(fill.WhenSettledAsync().IsCompleted);
        }
        finally
        {
            SystemMotion.IsReduced = false;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Waiting_for_the_fill_ends_when_it_stands_on_its_value()
    {
        var (window, bar, fill) = ShowBar(value: 0);
        bar.Value = 6;
        Dispatcher.UIThread.RunJobs();
        Tick();

        var settled = fill.WhenSettledAsync();
        Assert.False(settled.IsCompleted);

        for (var frame = 0; frame < 200 && !settled.IsCompleted; frame++)
        {
            Tick();
        }

        Assert.True(settled.IsCompleted);
        Assert.Equal(100, fill.DisplayedPercentage);

        window.Close();
    }

    /// <summary>A window that draws no frames (minimized) never shows the bar full; the curtain lifts anyway.</summary>
    [AvaloniaFact]
    public async Task The_curtain_waits_for_the_bar_at_most_its_limit()
    {
        var limit = TimeSpan.FromMilliseconds(300);
        var shell = StartupFailureTests.BuildShell(new QuickStep());

        var waited = Stopwatch.StartNew();
        await shell.RunStartupAsync(new StartupUiContext(new ContentControl())
        {
            WaitForProgressSettledAsync = () => new TaskCompletionSource().Task,
            ProgressSettleLimit = limit,
        });

        Assert.True(shell.IsReady);
        Assert.True(waited.Elapsed >= limit - TimeSpan.FromMilliseconds(20),
            $"Kurtyna zeszła po {waited.ElapsedMilliseconds} ms, przed limitem.");
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
    public async Task The_curtain_waits_for_the_bar_to_show_full()
    {
        var settled = new TaskCompletionSource();
        var shell = StartupFailureTests.BuildShell(new QuickStep());

        var startup = shell.RunStartupAsync(new StartupUiContext(new ContentControl())
        {
            WaitForProgressSettledAsync = () => settled.Task,
        });
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(10);
            Dispatcher.UIThread.RunJobs();
        }

        Assert.False(shell.IsReady);

        settled.SetResult();
        await startup;

        Assert.True(shell.IsReady);
    }

    [AvaloniaFact]
    public async Task Every_step_waits_for_a_frame()
    {
        var frames = 0;
        var shell = StartupFailureTests.BuildShell(new QuickStep(), new QuickStep());

        await shell.RunStartupAsync(new StartupUiContext(new ContentControl())
        {
            WaitForFrameAsync = () =>
            {
                frames++;
                return Task.CompletedTask;
            },
        });

        Assert.Equal(2, frames);
        Assert.True(shell.IsReady);
    }

    private static (Window Window, ProgressBar Bar, ProgressFill Fill) ShowBar(double value)
    {
        var bar = new ProgressBar { Width = 192, Minimum = 0, Maximum = 6, Value = value };
        var window = new Window { Width = 400, Height = 200, Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Tick();
        var fill = bar.GetVisualDescendants().OfType<ProgressFill>().Single();
        return (window, bar, fill);
    }

    private static void Tick()
    {
        Thread.Sleep(16);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class QuickStep : IStartupStep
    {
        public string Describe() => "Krok testowy";

        public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
