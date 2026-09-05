using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa jeden typ panelu deski (jego <see cref="Controls.Workspace.PanelWindow"/> i widok
/// wewnątrz), bez danych. Skonfigurowana fabryką kontrolki per typ panelu, żeby każdy typ trzymał wątek
/// dispatchera osobno zamiast zamrażać pasek postępu na jednym, długim kroku.
/// </summary>
public sealed class WarmPanelVisualStep(string message, Func<Control> createPanel) : IStartupStep
{
    public string Describe() => message;

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) =>
        VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, createPanel(), cancellationToken);
}
