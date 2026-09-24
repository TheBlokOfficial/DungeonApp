using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// A content tab as the library's own skeleton knows it (docs/architecture.md, "Zakładki treści"):
/// a title and the content types that fill it. A tab may name more than one content type, sharing
/// one list, one search box and one set of filters and sorts - today's two tabs each hold exactly
/// one, but <see cref="ContentListModel"/> never assumes that stays true.
/// </summary>
public sealed record ContentTabDefinition(string Title, IReadOnlyList<IContentTypeProfile> ContentTypes);
