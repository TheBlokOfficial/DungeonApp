using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>A layer-3 pack: entries and nothing else. See <see cref="SystemPack"/> for why this is a separate type.</summary>
public sealed record ContentPack(ContentId Id, string Name, PackVersion Version, IReadOnlyList<Entry> Entries);
