using System;
using System.Collections.Generic;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// The data blocks this build knows how to make - the data-side counterpart of
/// <see cref="Modules.ModuleCatalog"/>.
/// <para>
/// Unlike a module, a data block is data, not behaviour: there is nothing to instantiate, so this
/// registry holds shapes and versions rather than factories. It does not model dependencies either
/// - data blocks do not declare requirements on one another the way modules do.
/// </para>
/// </summary>
public sealed class DataBlockRegistry
{
    private readonly Dictionary<DataBlockId, DataBlockRegistration> _registrations = [];

    /// <summary>Returns itself so the composition root can list built-in data blocks in one expression.</summary>
    public DataBlockRegistry Register(DataBlockId id, int version, DataBlockShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        if (!_registrations.TryAdd(id, new DataBlockRegistration(id, version, shape)))
        {
            throw new ArgumentException($"Data block '{id}' is already registered.", nameof(id));
        }

        return this;
    }

    public bool Knows(DataBlockId id) => _registrations.ContainsKey(id);

    public DataBlockRegistration Describe(DataBlockId id) =>
        _registrations.TryGetValue(id, out var registration)
            ? registration
            : throw new InvalidOperationException($"Data block '{id}' is not part of this build.");
}
