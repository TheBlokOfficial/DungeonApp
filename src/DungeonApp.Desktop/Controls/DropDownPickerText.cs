using System;
using System.Collections.Generic;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Pure text rules of <see cref="DropDownPicker"/>: what the closed picker says, and which rows the
/// search keeps.
/// </summary>
public static class DropDownPickerText
{
    /// <summary>
    /// What the closed picker (and a filter chip) says: nothing selected - the filter's name; otherwise the first selected
    /// name (in the order of the list). The rest is <see cref="MoreBadge"/>.
    /// </summary>
    public static string Label(string? filterName, IReadOnlyList<string> selectedNames)
    {
        ArgumentNullException.ThrowIfNull(selectedNames);
        return selectedNames.Count == 0 ? filterName ?? string.Empty : selectedNames[0];
    }

    /// <summary>More than one selected - "+N" for the rest after the first; otherwise empty (no badge).</summary>
    public static string MoreBadge(IReadOnlyList<string> selectedNames)
    {
        ArgumentNullException.ThrowIfNull(selectedNames);
        return selectedNames.Count > 1 ? $"+{selectedNames.Count - 1}" : string.Empty;
    }

    /// <summary>
    /// More than one selected - the filter's name and every selected name, e.g. "Rzadkość: Rzadki,
    /// Bardzo rzadki, Legendarny"; otherwise none (a trimmed label shows its own full text).
    /// </summary>
    public static string? SelectionToolTip(string? filterName, IReadOnlyList<string> selectedNames)
    {
        ArgumentNullException.ThrowIfNull(selectedNames);
        if (selectedNames.Count < 2)
        {
            return null;
        }

        var names = string.Join(", ", selectedNames);
        return string.IsNullOrEmpty(filterName) ? names : $"{filterName}: {names}";
    }

    /// <summary>
    /// A row stays when its text contains the query, regardless of letter case (Polish letters
    /// included); an empty query keeps every row.
    /// </summary>
    public static bool Matches(string text, string? query)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.IsNullOrEmpty(query) || text.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
