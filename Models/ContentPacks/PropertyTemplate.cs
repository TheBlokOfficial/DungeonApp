namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon właściwości (Property Template) zdefiniowany w paczce.
/// Określa, jak aplikacja ma interpretować i wyświetlać daną właściwość.
/// </summary>
public class PropertyTemplate
{
    /// <summary>
    /// Pełna, czytelna dla człowieka nazwa właściwości (np. "Obrażenia", "Waga").
    /// Opcjonalna, domyślnie używany jest klucz ze słownika.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Opcjonalny format tekstowy, używający '%d%' jako zmiennej dla docelowej wartości.
    /// Przykłady: "%d% obrażeń", "%d% lb", "+%d% do ataku".
    /// Jeśli brak, wyświetlana jest surowa wartość.
    /// </summary>
    public string? DisplayFormat { get; set; }
}
