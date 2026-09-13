using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Core.Content;

/// <summary>
/// Resolves a <see cref="CampaignInstance"/> against a <see cref="ContentRegistry"/> - the read-time
/// answer to "what does this instance actually show right now", asked fresh on every read rather
/// than cached on the instance itself. That is what lets installing a pack the instance was already
/// pointing at repair it without anyone rewriting the campaign - see
/// <see cref="CampaignInstance.Source"/>.
/// <para>
/// Nothing here writes anything: not the instance, not its patch, not the campaign. A rejection
/// leaves <see cref="CampaignInstance.Patch"/> exactly as it arrived, because the alternative -
/// trimming or clearing a patch this resolver could not make sense of today - would make a later,
/// successful resolution silently different from the one the GM actually authored.
/// </para>
/// </summary>
public sealed class InstanceResolver
{
    private readonly IContentTypeCatalog _types;
    private readonly IReadOnlyDictionary<ContentId, Pack> _packsById;
    private readonly IReadOnlyDictionary<EntryAddress, RegisteredEntry> _entriesByAddress;

    public InstanceResolver(ContentRegistry registry, IContentTypeCatalog types)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(types);

        _types = types;

        // Built once, here, rather than scanned per instance: a campaign with hundreds of instances
        // resolved against a registry with hundreds of entries must stay linear, not quadratic.
        _packsById = registry.Packs.ToDictionary(pack => pack.Id);
        _entriesByAddress = registry.Entries.ToDictionary(entry => entry.Address);
    }

    public ResolvedInstance Resolve(CampaignInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!_packsById.ContainsKey(instance.Source.Pack))
        {
            return ResolvedInstance.CreateUnresolved(instance, source: null, InstanceUnresolvedReason.MissingPack);
        }

        if (!_entriesByAddress.TryGetValue(instance.Source, out var registered))
        {
            return ResolvedInstance.CreateUnresolved(instance, source: null, InstanceUnresolvedReason.MissingEntry);
        }

        if (registered.Unresolved is not null)
        {
            return ResolvedInstance.CreateUnresolved(instance, registered, InstanceUnresolvedReason.EntryUnresolved);
        }

        var merged = registered.Entry.Values.Overlay(instance.Patch);

        // Validation runs on the merged values, never the entry's own - the entry already passed
        // this same check when the registry loaded it, so the only thing worth asking about again
        // is whether the instance's own patch broke what the entry satisfied.
        if (!_types.TryValidate(registered.Entry.Type, merged, out var error))
        {
            return ResolvedInstance.CreateUnresolved(instance, registered, InstanceUnresolvedReason.ValuesRejected, error);
        }

        return ResolvedInstance.CreateResolved(instance, registered, merged);
    }
}
