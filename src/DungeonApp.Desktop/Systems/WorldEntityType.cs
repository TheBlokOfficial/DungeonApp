using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Systems;

/// <summary>
/// A content type whose entries stand in the world as entities. <paramref name="IconResourceKey"/> is
/// the fallback theme key for the tree row, used when the type gives no icon of its own
/// (<see cref="Entries.IContentPresentation.RowIcon"/>). <paramref name="CanBeAdded"/> says whether the
/// GM can put a new entity of the type in the tree; entities of a type that cannot be added (kept in
/// saved campaigns) still resolve and work.
/// </summary>
public sealed record WorldEntityType(ContentTypeReference Type, string IconResourceKey, bool CanBeAdded = true);
