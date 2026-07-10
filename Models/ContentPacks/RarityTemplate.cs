namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon rzadkości przedmiotu zdefiniowany w Paczce Zawartości.
/// </summary>
public class RarityTemplate
{
    /// <summary>
    /// Unikalny identyfikator rzadkości (np. "core_rarity_common").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Klucz tłumaczenia nazwy rzadkości (np. "core_rarity_common").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kolor przypisany do rzadkości w formacie HEX (np. "#808080").
    /// </summary>
    public string ColorHex { get; set; } = "#808080";

    /// <summary>
    /// Czy etykieta rzadkości w UI powinna mieć efekt blasku (np. dla legendarnych).
    /// </summary>
    public bool HasGlowEffect { get; set; } = false;
}
