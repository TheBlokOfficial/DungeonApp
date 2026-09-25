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
    /// Nothing selected - empty (the picker shows its placeholder); one - its name; more - the first
    /// name and "+N" for the rest, e.g. "Humanoid +2".
    /// </summary>
    public static string Summary(IReadOnlyList<string> selectedNames)
    {
        ArgumentNullException.ThrowIfNull(selectedNames);
        return selectedNames.Count switch
        {
            0 => string.Empty,
            1 => selectedNames[0],
            _ => $"{selectedNames[0]} +{selectedNames.Count - 1}",
        };
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
