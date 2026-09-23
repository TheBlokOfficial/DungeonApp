using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Library.Entries.Desktop.Startup;

/// <summary>
/// Wczytuje i waliduje paczki treści jednego systemu. Wynik trzyma we własnym polu i udostępnia jako
/// <see cref="Registry"/> - krok startowy, który ten system sam zgłasza ramie
/// (<c>IGameSystem.StartupSteps</c>, docs/architecture.md, "Rama, biblioteka, system": "system sam
/// wczytuje paczki i ma własny rejestr"). Rama tylko go uruchamia, nie wiedząc, co robi.
/// <para>
/// Nie ma tu własnego try/catch: <see cref="ContentPackLoader.LoadAsync"/> nigdy nie rzuca z powodu
/// wadliwej paczki - odkłada ją do <see cref="ContentRegistry.RejectedPacks"/> i ładuje dalej. Gdyby
/// mimo to rzucił, byłby to błąd loadera, a nie coś do wyciszenia tutaj - degradacja z
/// <c>AppShellViewModel.RunStartupAsync</c> ma to pokazać, nie druga, cichsza warstwa łapania.
/// </para>
/// </summary>
public sealed class LoadContentPacksStep(ContentPackLoader packLoader) : IStartupStep
{
    private ContentRegistry _registry = new([], [], [], []);

    public ContentRegistry Registry => _registry;

    public string Describe() => "Wczytywanie paczek treści…";

    public string FailureWarning => "Nie udało się wczytać paczek treści. Zostaną wczytane na żądanie.";

    public async Task PrepareAsync(CancellationToken cancellationToken) =>
        _registry = await packLoader.LoadAsync(cancellationToken);

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
