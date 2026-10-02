using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Odznaka: krótka wartość przy rzeczy - liczba, wyzwanie "1/2", "+3" - krojem liczb. Słowo
/// (kategoria, typ, rzadkość) to <see cref="WordTag"/>. Nieklikalna. Wygląd i odmiany po znaczeniu
/// (klasy .accent, .success, .warning, .danger, .custom) należą do motywu ramy
/// (Themes/Controls/BadgeAndWordTag.axaml).
/// </summary>
public sealed class Badge : ContentControl
{
}
