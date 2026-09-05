using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Modules;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// A module with no behaviour beyond declaring itself, for testing how a set of them is assembled.
/// It records its activation so the order the registry settled on can be asserted.
/// </summary>
internal sealed class StubModule(string id, params string[] requires) : ICampaignModule
{
    private static int _activations;

    public ModuleManifest Manifest { get; } = new(
        ModuleId.Create(id),
        DisplayName: id,
        StateVersion: 1,
        Requires: requires.Select(ModuleId.Create).ToArray());

    /// <summary>Null until activated; otherwise a globally increasing tick.</summary>
    public int? ActivatedAt { get; private set; }

    public ModuleContext? Context { get; private set; }

    public StubState State { get; private set; } = new(0);

    public Type StateType => typeof(StubState);

    public void OnActivated(ModuleContext context)
    {
        Context = context;
        ActivatedAt = ++_activations;
    }

    public object CaptureState() => State;

    public void RestoreState(object state, int version) => State = (StubState)state;
}

internal sealed record StubState(int Value);

/// <summary>A module that declares a dependency it can also be asked to resolve on activation.</summary>
internal sealed class DependentModule(string id, string requires) : ICampaignModule
{
    public ModuleManifest Manifest { get; } = new(
        ModuleId.Create(id),
        DisplayName: id,
        StateVersion: 1,
        Requires: [ModuleId.Create(requires)]);

    public StubModule? Resolved { get; private set; }

    public Type StateType => typeof(StubState);

    public void OnActivated(ModuleContext context) => Resolved = context.Modules.Get<StubModule>();

    public object CaptureState() => new StubState(0);

    public void RestoreState(object state, int version)
    {
    }
}
