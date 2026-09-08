namespace DungeonApp.Core.Content;

/// <summary>
/// One field a template declares: its address (<see cref="Id"/>), how it is labelled on a rendered
/// card, its scalar type, and whether an entry must supply a value for it.
/// <para>
/// <see cref="Required"/> defaults to <c>true</c> at the file format level - a pack author writes
/// <c>"required": false"</c> only to opt a field out, never the reverse - because most fields on a
/// real statblock are not optional and writing that out on every one of them would be pure noise.
/// </para>
/// </summary>
public sealed record FieldDeclaration(FieldName Id, string Label, FieldType Type, bool Required);
