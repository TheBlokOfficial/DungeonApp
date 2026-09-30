namespace DungeonApp.Core.Entries;

/// <summary>
/// A content type's row badge: opaque text for the library to print, plus an optional color key.
/// The key names an <em>intent</em> the system chose ("rzadkość: niezwykły", say) - never a color,
/// never anything the library looks inside. <see cref="ColorKey"/> travels through every
/// content-tab type in this project unread; only the view that draws a badge is allowed to turn it
/// into an actual color.
/// </summary>
public readonly record struct ContentBadge(string Text, string? ColorKey = null);
