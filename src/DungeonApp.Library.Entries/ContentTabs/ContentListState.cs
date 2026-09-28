using System.Collections.Generic;

namespace DungeonApp.Library.Entries;

/// <summary>
/// Everything about how a content tab is currently being looked at: the search box, the set of
/// values chosen in every filter, the chosen sort, and whichever row is selected. Every filter holds
/// a set: an empty set, a null one, or a missing key in <see cref="ValueFilters"/> all mean the
/// filter does not narrow. Values chosen in one filter combine with "or", filters with each other
/// (and with the search) with "and" (docs/architecture.md, "Zakładki treści"). Immutable: a caller
/// builds the next state and asks <see cref="ContentListModel.Build"/> for the next result, never
/// mutates this one in place.
/// </summary>
/// <param name="Categories">Chosen values of the category filter.</param>
/// <param name="Packs">Chosen pack names (a rejected pack by its location) of the "Paczka" filter.</param>
/// <param name="ValueFilters">Chosen values per value filter, keyed by the filter's label.</param>
public sealed record ContentListState(
    string Search = "",
    IReadOnlyCollection<string>? Categories = null,
    IReadOnlyCollection<string>? Packs = null,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>>? ValueFilters = null,
    string Sort = ContentListModel.DefaultSortLabel,
    ContentSelectionKey? Selected = null)
{
    /// <summary>
    /// Whether the search box or any filter narrows the list - what "Wyczyść filtry" has to clear.
    /// The sort never counts: it orders, it does not narrow.
    /// </summary>
    public bool AnyFilterNarrows =>
        Search.Length > 0
        || Categories is { Count: > 0 }
        || Packs is { Count: > 0 }
        || (ValueFilters is { } values && HasAnyValue(values));

    /// <summary>
    /// "Wyczyść filtry": every filter back to "nothing chosen", the search box empty. The sort is
    /// left exactly as it was - clearing filters is not a new sort choice - and so is
    /// <see cref="Selected"/>; whether a selection survives is decided the same way any other filter
    /// change decides it, by <see cref="ContentListModel.Build"/> itself.
    /// </summary>
    public ContentListState ClearFilters() => this with
    {
        Search = string.Empty,
        Categories = null,
        Packs = null,
        ValueFilters = null,
    };

    private static bool HasAnyValue(IReadOnlyDictionary<string, IReadOnlyCollection<string>> values)
    {
        foreach (var chosen in values.Values)
        {
            if (chosen.Count > 0)
            {
                return true;
            }
        }

        return false;
    }
}
