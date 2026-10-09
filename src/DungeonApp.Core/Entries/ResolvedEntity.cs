using System;
using DungeonApp.Core.Entries.Entities;

namespace DungeonApp.Core.Entries;

/// <summary>
/// The outcome of resolving one <see cref="CampaignEntity"/> against the registry: either the
/// registry record it points at and its values with the entity's patch overlaid, or the reason
/// it could not get that far - never both, never neither. The two factory methods are the only way
/// to build one, mirroring <see cref="RegisteredEntry"/>.
/// </summary>
public sealed record ResolvedEntity
{
    private ResolvedEntity(
        CampaignEntity entity,
        RegisteredEntry? source,
        ContentValues? values,
        EntityUnresolvedReason? unresolved,
        string? unresolvedDetail)
    {
        Entity = entity;
        Source = source;
        Values = values;
        Unresolved = unresolved;
        UnresolvedDetail = unresolvedDetail;
    }

    public CampaignEntity Entity { get; }

    /// <summary>
    /// The registry's own record for the entity's address, once the address at least resolves to
    /// one - present for <see cref="EntityUnresolvedReason.EntryUnresolved"/> and
    /// <see cref="EntityUnresolvedReason.ValuesRejected"/> as well as a resolved entity, null
    /// only for <see cref="EntityUnresolvedReason.MissingPack"/> and
    /// <see cref="EntityUnresolvedReason.MissingEntry"/>, where there is no record to carry.
    /// </summary>
    public RegisteredEntry? Source { get; }

    /// <summary>The entry's values with <see cref="CampaignEntity.Patch"/> overlaid. Present only when resolved.</summary>
    public ContentValues? Values { get; }

    public EntityUnresolvedReason? Unresolved { get; }

    /// <summary>
    /// The system's own explanation for refusing the merged values - populated only alongside
    /// <see cref="EntityUnresolvedReason.ValuesRejected"/>, the same text
    /// <see cref="IContentTypeCatalog.TryValidate"/> returned in its own <c>out string? error</c>.
    /// </summary>
    public string? UnresolvedDetail { get; }

    public static ResolvedEntity CreateResolved(CampaignEntity entity, RegisteredEntry source, ContentValues values)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(values);

        return new ResolvedEntity(entity, source, values, unresolved: null, unresolvedDetail: null);
    }

    public static ResolvedEntity CreateUnresolved(
        CampaignEntity entity, RegisteredEntry? source, EntityUnresolvedReason reason, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ResolvedEntity(entity, source, values: null, reason, detail);
    }
}
