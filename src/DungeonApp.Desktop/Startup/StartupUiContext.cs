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
}
