using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Systems;

/// <summary>
/// A content type whose entries can stand in the world as entities, and the icon the world tree
/// gives it. <paramref name="IconResourceKey"/> is the same theme key the type's library tab uses.
/// </summary>
public sealed record WorldEntityType(ContentTypeReference Type, string IconResourceKey);
