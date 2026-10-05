using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Badge: short value beside an object - number, challenge "1/2", "+3" - in the numeric font. A word
/// (category, type, rarity) uses <see cref="WordTag"/>. Non-clickable. Appearance and semantic variants
/// (.accent, .success, .warning, .danger, .custom classes) belong to the shell theme
/// (Themes/Controls/BadgeAndWordTag.axaml).
/// </summary>
public sealed class Badge : ContentControl
{
}
