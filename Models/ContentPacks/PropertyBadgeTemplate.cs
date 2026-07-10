using System.Text.Json.Serialization;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon pojedynczej odznaki właściwości wyświetlanej w tabelach UI (np. "Obrażenia", "Klasa Pancerza").
/// </summary>
public class PropertyBadgeTemplate
{
    /// <summary>
    /// Klucz ikony z wbudowanych zasobów Avalonia (np. "IconSword") LUB surowy ciąg ścieżki wektorowej SVG (np. "M10,20 L30,40 Z").
    /// Umożliwia używanie domyślnych systemowych ikon lub wprowadzanie własnych przez modderów.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Format tekstowy wspierający interpolację właściwości obiektu za pomocą Refleksji.
    /// Przykład: "{Weapon.DamageRoll} {Weapon.DamageType}" albo "+{Armor.ArmorBonus} AC".
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Kolor tekstu. Może być kluczem zasobu Avalonia (np. "TextPrimary", "AccentRed") lub kodem HEX (np. "#FF0000").
    /// Domyślnie używa koloru "TextPrimary".
    /// </summary>
    public string TextColor { get; set; } = "TextPrimary";
}
