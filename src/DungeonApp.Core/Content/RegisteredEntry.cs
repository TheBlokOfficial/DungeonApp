using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// One entry as the registry knows it: its address, its raw data, and either the content type it is
/// bound to or the reason binding failed - never both, never neither. The two factory methods are
/// the only way to build one, which is what makes that invariant unbreakable rather than merely
/// documented.
/// </summary>
public sealed record RegisteredEntry
{
    private RegisteredEntry(
        EntryAddress address, Entry entry, ContentTypeDescriptor? type, EntryUnresolvedReason? unresolved, string? unresolvedDetail)
    {
        Address = address;
        Entry = entry;
        Type = type;
        Unresolved = unresolved;
        UnresolvedDetail = unresolvedDetail;
    }

    public EntryAddress Address { get; }

    public Entry Entry { get; }

    /// <summary>The entry's content type, once <see cref="Unresolved"/> is null.</summary>
    public ContentTypeDescriptor? Type { get; }

    /// <summary>Why <see cref="Type"/> is null, once it is.</summary>
    public EntryUnresolvedReason? Unresolved { get; }

    /// <summary>
    /// Supplementary, reason-specific text - populated only alongside
    /// <see cref="EntryUnresolvedReason.ValuesRejected"/>, where it carries the content set's own
    /// explanation of what it rejected. It is the only channel the GM has for that explanation
    /// (see <see cref="IContentTypeCatalog.TryValidate"/>), so it is threaded all the way through
    /// to the registry screen rather than discarded once the entry is marked unresolved.
    /// </summary>
    public string? UnresolvedDetail { get; }

    public static RegisteredEntry CreateResolved(EntryAddress address, Entry entry, ContentTypeDescriptor type)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RegisteredEntry(address, entry, type, unresolved: null, unresolvedDetail: null);
    }

    public static RegisteredEntry CreateUnresolved(
        EntryAddress address, Entry entry, EntryUnresolvedReason reason, string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RegisteredEntry(address, entry, type: null, reason, detail);
    }
}
