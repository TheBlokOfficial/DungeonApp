using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Desktop.Shell.Workspace;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Rozgrzewa <see cref="WorkspacePlaceholderView"/> - widok pokazywany po wejściu w każdą sekcję
/// boczną poza kampaniami. Osobny krok, żeby ta sekcja miała swój skompilowany XAML gotowy niezależnie
/// od tego, czy na półce jest jakakolwiek kampania.
/// </summary>
public sealed class WarmWorkspacePlaceholderStep : IStartupStep
{
    public string Describe() => "Optymalizowanie pozostałych sekcji…";

    public Task PrepareAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) =>
        VisualWarmupHost.AttachAndWaitAsync(ui.WarmupHost, new WorkspacePlaceholderView(), cancellationToken);
}
