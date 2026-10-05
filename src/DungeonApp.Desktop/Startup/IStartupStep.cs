using System.Threading;
using System.Threading.Tasks;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// One step in the application startup sequence. The whole contract belongs to Desktop - <see
/// cref="ApplyAsync"/> touches the Avalonia tree, so Core remains unaware of startup.
/// Each step holds state needed between phases in its own private field, rather than a generic mechanism
/// shared by the contract.
/// </summary>
public interface IStartupStep
{
    /// <summary>Polish message shown to the user before the step starts.</summary>
    string Describe();

    /// <summary>Work without touching UI: disk reads, networking, computations.</summary>
    Task PrepareAsync(CancellationToken cancellationToken);

    /// <summary>Applies the step's result to the live UI tree.</summary>
    Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken);

    /// <summary>
    /// Status-bar message when this step fails and interrupts the startup sequence
    /// (the shell never knows the failure reason - the step names it). Default generic text suffices
    /// for shell steps; a system-provided step overrides it with its own specific text.
    /// </summary>
    string FailureWarning =>
        "Nie udało się w pełni przygotować startu aplikacji. Zostanie uruchomiona mimo to.";
}
