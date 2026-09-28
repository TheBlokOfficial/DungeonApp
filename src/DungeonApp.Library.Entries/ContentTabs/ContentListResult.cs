using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// The finished shape of one content tab for one <see cref="ContentListState"/>, ready to draw:
/// sections in display order, the current filter option lists, the available sort labels, the two
/// counters, and the surviving selection, if any. Every option list is drawn from all of the tab's
/// loaded entries, never from the ones the current filters leave - a choice never disappears from
/// under the hand while filtering.
/// </summary>
/// <param name="Category">The category filter, labelled by the system - null when no content type in the tab declares a category.</param>
/// <param name="Pack">The "Paczka" filter: pack names in the order sections list in, then rejected packs' locations.</param>
/// <param name="TotalCount">Every valid entry this tab holds, independent of the current filters.</param>
/// <param name="ShownCount">How many of <paramref name="TotalCount"/> are currently shown.</param>
public sealed record ContentListResult(
    IReadOnlyList<ContentSection> Sections,
    ContentFilterOptions? Category,
    ContentFilterOptions Pack,
    IReadOnlyList<ContentFilterOptions> ValueFilters,
    IReadOnlyList<string> Sorts,
    int TotalCount,
    int ShownCount,
    ContentSelectionDetail? Selection);
