using System.Collections.Generic;

namespace DungeonApp.Core.Entries;

/// <summary>
/// An installed pack: entries and nothing else. Content types live in compiled systems, not in
/// packs, so a pack has no kind.
/// </summary>
public sealed record Pack(ContentId Id, string Name, PackVersion Version, IReadOnlyList<Entry> Entries)
{
    /// <summary>
    /// The directory this pack was read from, as <see cref="ContentPackLoader"/> found it - the base
    /// an entry's picture path is resolved against (<see cref="EntryImagePath.Locate"/>).
    /// <see langword="null"/> for a pack built in memory rather than read from disk.
    /// </summary>
    public string? Location { get; init; }
}
