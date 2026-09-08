using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Wczytuje i waliduje paczki treści. Wynik trzyma we własnym polu i udostępnia jako <see
/// cref="Registry"/> - kolejny etap prac (ekran rejestru) będzie z niego czytał, tak jak <see
/// cref="WarmCampaignDataStep"/> czyta <see cref="LoadCampaignLibraryStep.Summaries"/>.
/// <para>
/// Nie ma tu własnego try/catch: <see cref="ContentPackLoader.LoadAsync"/> nigdy nie rzuca z powodu
/// wadliwej paczki - odkłada ją do <see cref="ContentRegistry.RejectedPacks"/> i ładuje dalej. Gdyby
/// mimo to rzucił, byłby to błąd loadera, a nie coś do wyciszenia tutaj - degradacja z <see
/// cref="Shell.AppShellViewModel.RunStartupAsync"/> ma to pokazać, nie druga, cichsza warstwa łapania.
/// </para>
/// </summary>
public sealed class LoadContentPacksStep(ContentPackLoader packLoader) : IStartupStep
{
    private ContentRegistry _registry = new([], [], [], []);

    public ContentRegistry Registry => _registry;

    public string Describe() => "Wczytywanie paczek treści…";

    public async Task PrepareAsync(CancellationToken cancellationToken) =>
        _registry = await packLoader.LoadAsync(cancellationToken);

    public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;
}
