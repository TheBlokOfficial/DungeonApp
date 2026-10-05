using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Tag: word beside an object - category, type, rarity ("Humanoid", "Rzadki") - in the UI font,
/// inside a pill. Values (number, challenge) use <see cref="Badge"/>. Non-clickable. Appearance and semantic
/// variants (.accent, .success, .warning, .danger, .custom classes) belong to the shell theme
/// (Themes/Controls/BadgeAndWordTag.axaml).
/// </summary>
public sealed class WordTag : ContentControl
{
}
