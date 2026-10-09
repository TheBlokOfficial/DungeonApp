using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Core.World;

/// <summary>
/// The state models the frame itself keeps for every campaign, whatever the system: entities, world
/// folders and the № counter. The tree belongs to the frame because it never asks what kind of thing
/// an entity is; a system adds only its own models on top (<see cref="Combine"/>).
/// </summary>
public static class WorldModels
{
    public static readonly StateModelDeclaration<WorldFolder> Folders = new("world.folders", 1);

    public static readonly StateModelDeclaration<WorldCounter> Counter = new("world.counter", 1);

    /// <summary>Every model the frame declares, in the order they are saved.</summary>
    public static IReadOnlyList<StateModelDeclaration> FrameModels { get; } =
        [EntitiesModel.Declaration, Folders, Counter];

    /// <summary>
    /// The one list of models a campaign keeps: the frame's own plus the system's. Everything that
    /// opens, creates or saves a campaign takes its declarations from here, so a model added to the
    /// frame reaches all of them at once. A system that redeclares a frame model is a programming
    /// error, not a silent override.
    /// </summary>
    public static IReadOnlyList<StateModelDeclaration> Combine(IReadOnlyList<StateModelDeclaration> systemModels)
    {
        ArgumentNullException.ThrowIfNull(systemModels);

        var frameIds = FrameModels.Select(model => model.ModelId).ToHashSet(StringComparer.Ordinal);

        if (systemModels.FirstOrDefault(model => frameIds.Contains(model.ModelId)) is { } clash)
        {
            throw new ArgumentException($"A system cannot declare the frame's state model '{clash.ModelId}'.", nameof(systemModels));
        }

        return [.. FrameModels, .. systemModels];
    }
}
