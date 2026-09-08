namespace DungeonApp.Core.Content;

/// <summary>
/// One line of a <see cref="StatblockElement"/>: the field it shows, and an optional second field
/// shown alongside it - "15" with its source "zbroja skórzana, tarcza" is one trait, not two, which
/// is why this is a pair rather than the statblock simply listing more fields.
/// </summary>
public sealed record StatblockTrait(FieldName Field, FieldName? Secondary);
