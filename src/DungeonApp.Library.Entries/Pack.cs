using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// An installed pack: entries and nothing else. Previously split into two record types keyed by a
/// <c>kind</c> field in <c>pack.json</c> - a "system" pack carrying templates, a "content" pack
/// carrying entries - which was the one thing in this layer anything ever branched on. Once content
/// types moved out of packs entirely and into compiled systems, a pack had nothing left to be
/// one of two kinds of, so the split - and the field that drove it - is gone.
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
