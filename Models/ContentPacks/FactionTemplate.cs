namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon określający Frakcję przeciwnika (grupowanie wizualne i logiczne).
/// </summary>
public class FactionTemplate
{
    /// <summary>
    /// Unikalny identyfikator frakcji (np. "core_faction_undead_scourge").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Pełna, wyświetlana nazwa frakcji.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kod wektorowy ikony (SVG) lub klucz z zasobów reprezentujący herb/znak frakcji.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Kolor przewodni frakcji w kodzie Hex (np. "#4B0082").
    /// </summary>
    public string ColorHex { get; set; } = "#808080";
}
