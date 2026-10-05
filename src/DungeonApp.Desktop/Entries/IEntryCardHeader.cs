using Avalonia.Controls;

namespace DungeonApp.Desktop.Entries;

/// <summary>
/// A card that lends part of itself to the entry's detail header. The header - breadcrumb, title,
/// tags - is drawn by the library, one look for every content type; a card that implements this
/// places up to four pieces into it:
/// <code>
/// breadcrumb (full width)
/// [HeaderVisual] | title ............ [HeaderTitleEnd]
///                | [HeaderTagsStart] tags
///                | HeaderBlock
/// </code>
/// The visual stands left of the title column with DungeonDetailColumnGap between them, and the
/// block sits at the foot of the title column, its bottom on the visual's bottom while the title
/// column is shorter than the visual - unless the card keeps it under the tags
/// (<see cref="HeaderBlockFollowsTitle"/>). The title's end stands against the column's right edge,
/// centred on the title's first line when it is one line tall; a taller one keeps its own first
/// line on the title's (by centring it within one line of the title's style) and runs down beside
/// the tags. A long title (unless kept to one line, <see cref="HeaderTitleOnOneLine"/>) and the tags
/// both wrap before it, and nothing else moves. The tags' start
/// comes before the first tag, in the same wrapping row. Any piece may be null; a card that does
/// not implement this gets the header with none. Each piece is a control of its own, never a part of
/// the card's own tree - a control has one parent.
/// </summary>
public interface IEntryCardHeader
{
    /// <summary>A picture's frame, as tall as the card wants it; null - nothing beside the title.</summary>
    Control? HeaderVisual { get; }

    /// <summary>A short value at the end of the title's first line; null - the title has the line to itself.</summary>
    Control? HeaderTitleEnd { get; }

    /// <summary>A word drawn before the tags, in their row (a tier the card colors itself); null - nothing.</summary>
    Control? HeaderTagsStart { get; }

    /// <summary>The card's headline values under the tags; null - nothing there.</summary>
    Control? HeaderBlock { get; }

    /// <summary>
    /// Whether the title keeps to one line, trimmed with an ellipsis and shown whole in place under
    /// the pointer (<see cref="DungeonApp.Desktop.Controls.RevealingTextBlock"/>) - for a card whose visual leaves room
    /// beside it for one line of title only. False - the title wraps, as it does without a card.
    /// </summary>
    bool HeaderTitleOnOneLine => false;

    /// <summary>
    /// Whether the block follows the tags directly instead of sitting at the column's foot - for a
    /// block that is prose about the name (a summary), which reads as detached when pushed down to the
    /// visual's bottom edge. False - the foot, where headline values line up with the visual.
    /// </summary>
    bool HeaderBlockFollowsTitle => false;
}
