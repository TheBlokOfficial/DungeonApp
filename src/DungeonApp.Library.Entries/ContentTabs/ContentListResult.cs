using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// The finished shape of one content tab for one <see cref="ContentListState"/>, ready to draw:
/// sections in display order, the current filter option lists, the available sort labels, the two
/// counters, and the surviving selection, if any.
/// </summary>
/// <param name="TotalCount">Every valid entry this tab holds, independent of the current filters.</param>
/// <param name="ShownCount">How many of <paramref name="TotalCount"/> are currently shown.</param>
public sealed record ContentListResult(
    IReadOnlyList<ContentSection> Sections,
    ContentFilterOptions Category,
    ContentFilterOptions Source,
    IReadOnlyList<ContentFilterOptions> ValueFilters,
    IReadOnlyList<string> Sorts,
    int TotalCount,
    int ShownCount,
    ContentSelectionDetail? Selection);
