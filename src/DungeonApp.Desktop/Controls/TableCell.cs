using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Komórka <see cref="Table"/>: wcięcie, wyrównanie treści (HorizontalContentAlignment), tło
/// wyróżnienia (Background podany przez układającego - wypełnia komórkę do linii). Linie rysuje
/// sama komórka, ale ich grubość ustala tabela z położenia komórki (<see cref="Table"/>). Wygląd
/// i klasy (.header - wiersz nagłówka, .mono - liczby krojem liczb) należą do motywu ramy
/// (Themes/DungeonControls.axaml).
/// </summary>
public sealed class TableCell : ContentControl
{
}
