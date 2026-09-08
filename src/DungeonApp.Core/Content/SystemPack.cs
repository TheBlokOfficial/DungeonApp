using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>
/// A layer-2 pack: templates and nothing else. Deliberately its own type rather than one
/// <c>Pack</c> record with a "kind" flag - the two kinds of pack carry different content
/// (<see cref="Template"/> versus <see cref="Content.Entry"/>) and are never handled by the same
/// code path, so a shared type would only invite the one thing this layer must not do: branching on
/// what kind of thing a pack (or, worse, an entry) is.
/// </summary>
public sealed record SystemPack(ContentId Id, string Name, PackVersion Version, IReadOnlyList<Template> Templates);
