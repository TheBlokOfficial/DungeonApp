using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// One entry as the registry knows it: its address, its raw data, and either the template it is
/// bound to or the reason binding failed - never both, never neither. The two factory methods are
/// the only way to build one, which is what makes that invariant unbreakable rather than merely
/// documented.
/// </summary>
public sealed record RegisteredEntry
{
    private RegisteredEntry(EntryAddress address, Entry entry, Template? template, EntryUnresolvedReason? unresolved)
    {
        Address = address;
        Entry = entry;
        Template = template;
        Unresolved = unresolved;
    }

    public EntryAddress Address { get; }

    public Entry Entry { get; }

    /// <summary>The entry's template, once <see cref="Unresolved"/> is null.</summary>
    public Template? Template { get; }

    /// <summary>Why <see cref="Template"/> is null, once it is.</summary>
    public EntryUnresolvedReason? Unresolved { get; }

    public static RegisteredEntry CreateResolved(EntryAddress address, Entry entry, Template template)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(template);

        return new RegisteredEntry(address, entry, template, unresolved: null);
    }

    public static RegisteredEntry CreateUnresolved(EntryAddress address, Entry entry, EntryUnresolvedReason reason)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RegisteredEntry(address, entry, template: null, reason);
    }
}
