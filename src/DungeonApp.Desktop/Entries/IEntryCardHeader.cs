using Avalonia.Controls;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// A card that lends part of itself to the entry's detail header. The header - breadcrumb, title,
/// tags - is drawn by the library, one look for every content type; a card that implements this
/// places up to four pieces into it:
/// <code>
/// breadcrumb (full width)
/// [HeaderVisual] | title ............ [HeaderTitleEnd]
///                | tags [HeaderTagsEnd]
///                | HeaderBlock
/// </code>
/// The visual stands left of the title column with DungeonDetailColumnGap between them, and the
/// block sits at the foot of the title column, its bottom on the visual's bottom while the title
/// column is shorter than the visual. The title's end stands against the column's right edge,
/// centred on the title's first line; a long title wraps before it, and nothing else moves. The
/// tags' end follows the last tag in the same wrapping row. Any piece may be null; a card that does
/// not implement this gets the header with none. Each piece is a control of its own, never a part of
/// the card's own tree - a control has one parent.
/// </summary>
public interface IEntryCardHeader
{
    /// <summary>A picture's frame, as tall as the card wants it; null - nothing beside the title.</summary>
    Control? HeaderVisual { get; }

    /// <summary>A short value at the end of the title's first line; null - the title has the line to itself.</summary>
    Control? HeaderTitleEnd { get; }

    /// <summary>A word drawn after the tags, in their row (a tier the card colors itself); null - nothing.</summary>
    Control? HeaderTagsEnd { get; }

    /// <summary>The card's headline values under the tags; null - nothing there.</summary>
    Control? HeaderBlock { get; }
}
