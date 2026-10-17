using System.Collections.Generic;

namespace DungeonApp.Desktop.Entries.ContentTab;

/// <summary>
/// The path an entry's detail block shows over its name: "{tytuł zakładki} › {kategoria} › {nazwa}",
/// the category left out when the entry has none. One builder for the library and for an entity's
/// card, so the same entry reads the same wherever it is drawn.
/// </summary>
public static class EntryPath
{
    public static IReadOnlyList<string> Of(string tabTitle, string? category, string entryName) =>
        category is { } named ? [tabTitle, named, entryName] : [tabTitle, entryName];
}
