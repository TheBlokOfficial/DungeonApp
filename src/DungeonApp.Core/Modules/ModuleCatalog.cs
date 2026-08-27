using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.Modules;

/// <summary>
/// The modules this build knows how to make. Every entry is compiled in; the catalogue exists so
/// the composition root names them in one place, not so they can be discovered at runtime.
/// <para>
/// Reopening a campaign has to build fresh module instances rather than revive stored objects,
/// which is why a factory is registered instead of an instance.
/// </para>
/// </summary>
public sealed class ModuleCatalog
{
    private readonly Dictionary<ModuleId, Func<ICampaignModule>> _factories = [];
    private readonly List<ModuleManifest> _manifests = [];

    /// <summary>Returns itself so the composition root can list the built-in modules in one expression.</summary>
    public ModuleCatalog Register(ModuleId id, Func<ICampaignModule> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.ContainsKey(id))
        {
            throw new ArgumentException($"Module '{id}' is already registered.", nameof(id));
        }

        // One instance is built here and thrown away. A module is inert until a campaign activates
        // it, so this costs nothing and buys two things: the manifest can be shown before any
        // campaign exists, and a misfiled factory fails at startup rather than mid-session.
        var manifest = factory().Manifest;

        if (manifest.Id != id)
        {
            throw new ArgumentException(
                $"Module registered as '{id}' reports its id as '{manifest.Id}'.", nameof(factory));
        }

        _factories.Add(id, factory);
        _manifests.Add(manifest);

        return this;
    }

    /// <summary>
    /// Whether a stored campaign naming this module can be opened at all. A save may legitimately
    /// mention modules this build has never heard of.
    /// </summary>
    public bool Knows(ModuleId id) => _factories.ContainsKey(id);

    public IReadOnlyCollection<ModuleId> Known => _factories.Keys;

    /// <summary>
    /// What a campaign can be given, in registration order, so the GM is offered the modules in the
    /// order the composition root meant rather than in dictionary order.
    /// </summary>
    public IReadOnlyList<ModuleManifest> Manifests => _manifests;

    public ModuleManifest Describe(ModuleId id) =>
        _manifests.FirstOrDefault(manifest => manifest.Id == id)
        ?? throw new InvalidOperationException($"Module '{id}' is not part of this build.");

    /// <summary>
    /// Adds whatever the chosen modules declared they need, and whatever those need in turn.
    /// <para>
    /// A GM ticking the scheduler is not making a statement about the clock; they are asking for
    /// something that cannot work without one. Resolving that here means the choice offered on the
    /// screen and the set handed to <see cref="Campaigns.CreateCampaign"/> are the same set, and the
    /// campaign is never assembled only to be refused for a dependency the GM never saw.
    /// </para>
    /// </summary>
    public IReadOnlyList<ModuleId> WithRequirements(IEnumerable<ModuleId> selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var closed = new HashSet<ModuleId>();
        var pending = new Queue<ModuleId>(selection);

        while (pending.Count > 0)
        {
            var id = pending.Dequeue();

            if (!closed.Add(id))
            {
                continue;
            }

            foreach (var required in Describe(id).Requires)
            {
                pending.Enqueue(required);
            }
        }

        // Registration order, not discovery order: the same selection must always produce the same
        // list, whatever order the GM ticked the boxes in.
        return [.. _manifests.Where(manifest => closed.Contains(manifest.Id)).Select(manifest => manifest.Id)];
    }

    public ICampaignModule Create(ModuleId id)
    {
        if (!_factories.TryGetValue(id, out var factory))
        {
            throw new InvalidOperationException($"Module '{id}' is not part of this build.");
        }

        var module = factory();

        // A factory filed under the wrong id would corrupt state files by writing one module's
        // state under another's name. Cheap to check, and impossible to spot later.
        if (module.Manifest.Id != id)
        {
            throw new InvalidOperationException(
                $"Module registered as '{id}' reports its id as '{module.Manifest.Id}'.");
        }

        return module;
    }
}
