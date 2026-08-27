using System;
using System.Collections.Generic;

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

    /// <summary>Returns itself so the composition root can list the built-in modules in one expression.</summary>
    public ModuleCatalog Register(ModuleId id, Func<ICampaignModule> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (!_factories.TryAdd(id, factory))
        {
            throw new ArgumentException($"Module '{id}' is already registered.", nameof(id));
        }

        return this;
    }

    /// <summary>
    /// Whether a stored campaign naming this module can be opened at all. A save may legitimately
    /// mention modules this build has never heard of.
    /// </summary>
    public bool Knows(ModuleId id) => _factories.ContainsKey(id);

    public IReadOnlyCollection<ModuleId> Known => _factories.Keys;

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
