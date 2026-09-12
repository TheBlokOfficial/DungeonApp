namespace DungeonApp.Core.Content;

/// <summary>
/// The same idea as <see cref="RejectedPack"/>, one level down: one entry file inside an otherwise
/// accepted pack that the loader refused to read as an entry at all, and why.
/// <para>
/// <see cref="Reason"/> is diagnostic text - it exists to name a file and a concrete defect, never
/// to just say "invalid entry". Unlike a rejected pack, a rejected entry never takes any sibling
/// down with it: the rest of <see cref="Pack"/> keeps loading and registering normally.
/// </para>
/// </summary>
public sealed record RejectedEntry(ContentId Pack, string Location, string Reason);
