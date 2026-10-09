using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries.Entities;

namespace DungeonApp.Core.Entries;

/// <summary>
/// Resolves a <see cref="CampaignEntity"/> against a <see cref="ContentRegistry"/> - the read-time
/// answer to "what does this entity actually show right now", asked fresh on every read rather
/// than cached on the entity itself. That is what lets installing a pack the entity was already
/// pointing at repair it without anyone rewriting the campaign - see
/// <see cref="CampaignEntity.Source"/>.
/// <para>
/// Nothing here writes anything: not the entity, not its patch, not the campaign. A rejection
/// leaves <see cref="CampaignEntity.Patch"/> exactly as it arrived, because the alternative -
/// trimming or clearing a patch this resolver could not make sense of today - would make a later,
/// successful resolution silently different from the one the GM actually authored.
/// </para>
/// </summary>
public sealed class EntityResolver
{
    private readonly IContentTypeCatalog _types;
    private readonly IReadOnlyDictionary<ContentId, Pack> _packsById;
    private readonly IReadOnlyDictionary<EntryAddress, RegisteredEntry> _entriesByAddress;

    public EntityResolver(ContentRegistry registry, IContentTypeCatalog types)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(types);

        _types = types;

        // Built once, here, rather than scanned per entity: a campaign with hundreds of entities
        // resolved against a registry with hundreds of entries must stay linear, not quadratic.
        _packsById = registry.Packs.ToDictionary(pack => pack.Id);
        _entriesByAddress = registry.Entries.ToDictionary(entry => entry.Address);
    }

    public ResolvedEntity Resolve(CampaignEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_packsById.ContainsKey(entity.Source.Pack))
        {
            return ResolvedEntity.CreateUnresolved(entity, source: null, EntityUnresolvedReason.MissingPack);
        }

        if (!_entriesByAddress.TryGetValue(entity.Source, out var registered))
        {
            return ResolvedEntity.CreateUnresolved(entity, source: null, EntityUnresolvedReason.MissingEntry);
        }

        if (registered.Unresolved is not null)
        {
            return ResolvedEntity.CreateUnresolved(entity, registered, EntityUnresolvedReason.EntryUnresolved);
        }

        var merged = registered.Entry.Values.Overlay(entity.Patch);

        // Validation runs on the merged values, never the entry's own - the entry already passed
        // this same check when the registry loaded it, so the only thing worth asking about again
        // is whether the entity's own patch broke what the entry satisfied.
        if (!_types.TryValidate(registered.Entry.Type, merged, out var error))
        {
            return ResolvedEntity.CreateUnresolved(entity, registered, EntityUnresolvedReason.ValuesRejected, error);
        }

        return ResolvedEntity.CreateResolved(entity, registered, merged);
    }
}
