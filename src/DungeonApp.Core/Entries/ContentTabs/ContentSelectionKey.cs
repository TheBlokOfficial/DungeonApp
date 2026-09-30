namespace DungeonApp.Core.Entries;

/// <summary>
/// Identifies a selected row in a content tab, stably across a rebuild triggered by a filter or
/// search change. The three shapes mirror the three things a selection can be
/// (docs/architecture.md, "Zakładki treści"): a resolved or unresolved <see cref="RegisteredEntry"/>
/// (both always carry an <see cref="EntryAddress"/>), a <see cref="RejectedEntry"/> file that never
/// became an entry at all, or a <see cref="RejectedPack"/>'s own header row.
/// </summary>
public abstract record ContentSelectionKey;

/// <summary>Selects a resolved or unresolved entry by its address.</summary>
public sealed record EntrySelectionKey(EntryAddress Address) : ContentSelectionKey;

/// <summary>Selects a file that never became an entry, by the pack it lives in and its location.</summary>
public sealed record RejectedFileSelectionKey(ContentId Pack, string Location) : ContentSelectionKey;

/// <summary>Selects a rejected pack's own header row, by its location.</summary>
public sealed record RejectedPackSelectionKey(string Location) : ContentSelectionKey;
