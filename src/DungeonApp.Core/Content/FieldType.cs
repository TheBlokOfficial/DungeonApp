namespace DungeonApp.Core.Content;

/// <summary>
/// The scalar kinds a template field can declare. Closed on purpose, the same way
/// <see cref="DataBlocks.PrimitiveKind"/> is closed: v1 only ever needs to tell a rules-book number
/// apart from a rules-book sentence, and widening this is a catalog decision, not something a pack
/// can do by writing a new string.
/// </summary>
public enum FieldType
{
    Text,
    Integer
}
