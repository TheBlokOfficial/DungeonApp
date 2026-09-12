namespace DungeonApp.Core.Content;

/// <summary>
/// An entry's address in the registry: which pack it came from, and its id inside that pack.
/// Written the same way a <see cref="ContentTypeReference"/> is - <c>"pack:entry"</c> - because both
/// are the same idea (a two-part, colon-joined pointer into the registry), just pointing at a
/// different kind of thing.
/// </summary>
public readonly record struct EntryAddress(ContentId Pack, ContentId Entry)
{
    public override string ToString() => $"{Pack}:{Entry}";
}
