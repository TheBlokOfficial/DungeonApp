using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// The outcome of resolving one <see cref="CampaignInstance"/> against the registry: either the
/// registry record it points at and its values with the instance's patch overlaid, or the reason
/// it could not get that far - never both, never neither. The two factory methods are the only way
/// to build one, mirroring <see cref="RegisteredEntry"/>.
/// </summary>
public sealed record ResolvedInstance
{
    private ResolvedInstance(
        CampaignInstance instance,
        RegisteredEntry? source,
        ContentValues? values,
        InstanceUnresolvedReason? unresolved,
        string? unresolvedDetail)
    {
        Instance = instance;
        Source = source;
        Values = values;
        Unresolved = unresolved;
        UnresolvedDetail = unresolvedDetail;
    }

    public CampaignInstance Instance { get; }

    /// <summary>
    /// The registry's own record for the instance's address, once the address at least resolves to
    /// one - present for <see cref="InstanceUnresolvedReason.EntryUnresolved"/> and
    /// <see cref="InstanceUnresolvedReason.ValuesRejected"/> as well as a resolved instance, null
    /// only for <see cref="InstanceUnresolvedReason.MissingPack"/> and
    /// <see cref="InstanceUnresolvedReason.MissingEntry"/>, where there is no record to carry.
    /// </summary>
    public RegisteredEntry? Source { get; }

    /// <summary>The entry's values with <see cref="CampaignInstance.Patch"/> overlaid. Present only when resolved.</summary>
    public ContentValues? Values { get; }

    public InstanceUnresolvedReason? Unresolved { get; }

    /// <summary>
    /// The content set's own explanation for refusing the merged values - populated only alongside
    /// <see cref="InstanceUnresolvedReason.ValuesRejected"/>, the same text
    /// <see cref="IContentTypeCatalog.TryValidate"/> returned in its own <c>out string? error</c>.
    /// </summary>
    public string? UnresolvedDetail { get; }

    public static ResolvedInstance CreateResolved(CampaignInstance instance, RegisteredEntry source, ContentValues values)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(values);

        return new ResolvedInstance(instance, source, values, unresolved: null, unresolvedDetail: null);
    }

    public static ResolvedInstance CreateUnresolved(
        CampaignInstance instance, RegisteredEntry? source, InstanceUnresolvedReason reason, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new ResolvedInstance(instance, source, values: null, reason, detail);
    }
}
