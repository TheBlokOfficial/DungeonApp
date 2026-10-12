using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Minimal UI reference carrier for startup steps - enough for the <see
/// cref="IStartupStep.ApplyAsync"/> phase to remain unaware of the <c>AppShellView</c> type.
/// </summary>
public sealed class StartupUiContext(ContentControl warmupHost)
{
    /// <summary>Invisible host where visual steps attach their controls during warmup.</summary>
    public ContentControl WarmupHost { get; } = warmupHost;

    /// <summary>
    /// Awaited after every step, before the next one takes the UI thread: the curtain's bar starts
    /// gliding towards the new value in a drawn frame. Without a window, one pass of the dispatcher.
    /// </summary>
    public Func<Task> WaitForFrameAsync { get; init; } =
        () => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();

    /// <summary>
    /// Completes once the curtain's bar shows full, so the GM sees it reach its end before the
    /// curtain lifts. At once when there is no bar or system animations are disabled.
    /// </summary>
    public Func<Task> WaitForProgressSettledAsync { get; init; } = () => Task.CompletedTask;

    /// <summary>
    /// The longest the curtain waits for <see cref="WaitForProgressSettledAsync"/>: a window that
    /// draws no frames (minimized) never shows the bar full.
    /// </summary>
    public TimeSpan ProgressSettleLimit { get; init; } = TimeSpan.FromSeconds(1);
}
