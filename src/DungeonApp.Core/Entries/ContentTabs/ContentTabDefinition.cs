using System.Collections.Generic;

namespace DungeonApp.Core.Entries;

/// <summary>
/// A content tab as the library's own skeleton knows it (docs/architecture.md, "Zakładki treści"):
/// a title and the content types that fill it. A tab may name more than one content type, sharing
/// one list, one search box and one set of filters and sorts - today's two tabs each hold exactly
/// one, but <see cref="ContentListModel"/> never assumes that stays true.
/// <para>
/// <see cref="EmptyText"/> is the system's own sentence for a tab no pack fills yet ("Żadna paczka
/// nie ma jeszcze potworów.") - the skeleton knows no content type's name, so it cannot write one.
/// Null falls back to a sentence that names no type.
/// </para>
/// </summary>
public sealed record ContentTabDefinition(
    string Title, IReadOnlyList<IContentTypeProfile> ContentTypes, string? EmptyText = null);
