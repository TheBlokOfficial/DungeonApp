using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// Everything about how a content tab is currently being looked at: the search box, one selected
/// option per filter (null - or a missing key in <see cref="ValueFilters"/> - means "wszystkie"),
/// the chosen sort, and whichever row is selected. Immutable: a caller builds the next state and
/// asks <see cref="ContentListModel.Build"/> for the next result, never mutates this one in place.
/// </summary>
public sealed record ContentListState(
    string Search = "",
    string? Category = null,
    string? Source = null,
    IReadOnlyDictionary<string, string>? ValueFilters = null,
    string Sort = ContentListModel.DefaultSortLabel,
    ContentSelectionKey? Selected = null)
{
    /// <summary>
    /// "Wyczyść filtry": every filter back to "wszystkie", the search box empty. The sort is left
    /// exactly as it was - clearing filters is not a new sort choice - and so is
    /// <see cref="Selected"/>; whether a selection survives is decided the same way any other filter
    /// change decides it, by <see cref="ContentListModel.Build"/> itself.
    /// </summary>
    public ContentListState ClearFilters() => this with
    {
        Search = string.Empty,
        Category = null,
        Source = null,
        ValueFilters = null,
    };
}
