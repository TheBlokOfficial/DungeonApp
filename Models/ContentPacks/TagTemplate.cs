using System.Text.Json.Serialization;

namespace DungeonApp.Models.ContentPacks;

public class TagTemplate
{
    /// <summary>
    /// Unikalny identyfikator tagu (np. "boss").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Kolor tła w formacie HEX (np. "#4C1D95").
    /// </summary>
    public string ColorHex { get; set; } = string.Empty;
}
