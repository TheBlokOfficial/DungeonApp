using Avalonia.Controls;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Minimalny nośnik referencji UI dla kroków startowych - tyle, żeby faza <see
/// cref="IStartupStep.ApplyAsync"/> nie musiała znać typu <c>AppShellView</c>.
/// </summary>
public sealed class StartupUiContext(ContentControl warmupHost)
{
    /// <summary>Niewidoczny host, do którego kroki wizualne podpinają swoje kontrolki na czas rozgrzewki.</summary>
    public ContentControl WarmupHost { get; } = warmupHost;
}
