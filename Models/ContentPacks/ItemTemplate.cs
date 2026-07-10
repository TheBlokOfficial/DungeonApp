using System.Collections.Generic;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon przedmiotu zdefiniowany w Paczce Zawartości (Content Pack).
/// </summary>
/// <remarks>
/// Jest to byt PASYWNY – nigdy nie jest modyfikowany przez gracza ani DM-a w trakcie gry.
/// Ekwipunek postaci przechowuje jedynie referencję (TemplateId) do tego szablonu.
/// Dzięki temu aktualizacja paczki (np. zmiana obrażeń miecza) natychmiast
/// propaguje się do wszystkich postaci posiadających ten przedmiot.
///
/// System własności (Properties) jest inspirowany mechaniką NBT z Minecrafta –
/// zamiast sztywnych pól (Damage, Armor), przedmiot posiada dynamiczny słownik
/// par klucz-wartość, dzięki czemu może reprezentować dowolny typ obiektu
/// (broń, zbroję, miksturę, zwój) bez konieczności tworzenia osobnych klas.
/// </remarks>
public class ItemTemplate
{
    /// <summary>
    /// Unikalny identyfikator przedmiotu w obrębie paczki (np. "longsword").
    /// </summary>
    /// <remarks>
    /// Pełny, globalnie unikalny identyfikator jest budowany przez ContentRegistry
    /// w formacie "[PackId]:[ItemId]" (np. "dnd_core:longsword").
    /// Dzięki temu dwie różne paczki mogą posiadać przedmiot o nazwie "Fire Sword"
    /// bez kolizji identyfikatorów.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Opcjonalne referencyjne ID do innego przedmiotu (np. "core:weapon_base").
    /// Pozwala na dziedziczenie właściwości, formatów i tagów.
    /// </summary>
    public string? BaseTemplate { get; set; }

    /// <summary>
    /// Wyświetlana nazwa przedmiotu (np. "Miecz Długi").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Opis fabularny przedmiotu (wyświetlany w podglądzie szczegółów).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Główna kategoria/typ przedmiotu (np. "core_type_weapon", "core_type_armor").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Rzadkość przedmiotu (np. "core_rarity_common", "core_rarity_legendary").
    /// </summary>
    public string Rarity { get; set; } = "core_rarity_common";

    /// <summary>
    /// Waga przedmiotu (do celów obliczania ekwipunku).
    /// </summary>
    public double Weight { get; set; } = 0.0;

    /// <summary>
    /// Tagi opisujące naturę przedmiotu (np. "Magic", "Cursed").
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Komponenty mechaniczne (klocki) przypisane do przedmiotu.
    /// Zastępują stary słownik Properties.
    /// </summary>
    public DungeonApp.Models.ContentPacks.Components.ItemComponents Components { get; set; } = new();

    /// <summary>
    /// Opcjonalna lista własnych szablonów formatowania właściwości.
    /// Jeśli zdefiniowana, ignoruje format określony przez kategorię przedmiotu (Type).
    /// </summary>
    public List<PropertyBadgeTemplate>? RegistryFormat { get; set; }
}
