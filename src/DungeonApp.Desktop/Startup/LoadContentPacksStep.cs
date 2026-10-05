using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Loads and validates one system's content packs. Holds the result in its own field and exposes
/// <see cref="Registry"/> - startup step supplied to the shell by the system itself
/// (<c>IGameSystem.StartupSteps</c>). The shell only runs it, without knowing what it does.
/// <para>
/// No local try/catch: <see cref="ContentPackLoader.LoadAsync"/> never throws for
/// an invalid pack - adds it to <see cref="ContentRegistry.RejectedPacks"/> and continues loading. Any
/// exception would be a loader bug, not something to silence here - degraded startup in
/// <c>AppShellViewModel.RunStartupAsync</c> should show it rather than a second, quieter catch layer.
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
