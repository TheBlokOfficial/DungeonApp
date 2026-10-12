using System;
using Avalonia.Controls;

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
    /// How long the curtain's progress bar takes to slide to a new value. After the last step the
    /// curtain stays this long, so the GM sees the bar reach its end; zero when system animations
    /// are disabled (the bar jumps, nothing to wait for).
    /// </summary>
    public TimeSpan ProgressSettleTime { get; init; } = TimeSpan.Zero;
}
