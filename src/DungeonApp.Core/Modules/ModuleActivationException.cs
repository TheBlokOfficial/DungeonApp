using System;

namespace DungeonApp.Core.Modules;

/// <summary>Why a set of modules cannot form a campaign.</summary>
public enum ModuleActivationFailure
{
    /// <summary>Two modules claim the same id.</summary>
    DuplicateModule,

    /// <summary>A module declares a dependency that is not switched on.</summary>
    MissingDependency,

    /// <summary>Modules depend on each other in a loop, so no activation order exists.</summary>
    CircularDependency
}

/// <summary>
/// Raised while assembling a campaign's modules, never during a session: an impossible combination
/// is refused up front rather than discovered halfway through an operation at the table.
/// </summary>
public sealed class ModuleActivationException(ModuleActivationFailure failure, string message)
    : Exception(message)
{
    public ModuleActivationFailure Failure { get; } = failure;
}
