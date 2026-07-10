using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Paczka Zawartości (Content Pack) – uniwersalny kontener zasobów gry.
/// </summary>
/// <remarks>
/// Reprezentuje fizyczny folder na dysku użytkownika (np. Documents/DungeonApp/Packs/dnd_core/).
/// Każda paczka może zawierać dowolną kombinację przedmiotów, potworów i scenariuszy fabularnych.
///
/// Architektura inspirowana systemem Resource Packów z Minecrafta:
/// - Paczka jest bytem PASYWNYM (biblioteką) – nigdy nie jest modyfikowana przez gracza.
/// - Kampania (byt AKTYWNY / plik zapisu gry) jedynie wskazuje, które paczki są aktywne.
/// - Dzięki temu jeden folder paczki może być współdzielony przez wiele kampanii,
///   a aktualizacja paczki (np. naprawa literówki w opisie miecza) natychmiast
///   propaguje się do wszystkich gier, które z niej korzystają.
/// </remarks>
public class ContentPack
{
    /// <summary>
    /// Unikalny identyfikator paczki (np. "dnd_core").
    /// </summary>
    /// <remarks>
    /// Odpowiada nazwie folderu na dysku (np. Documents/DungeonApp/packs/dnd_core/).
    /// Jest używany jako prefiks w pełnych identyfikatorach przedmiotów
    /// (np. "dnd_core:longsword"), co eliminuje konflikty nazw między paczkami.
    /// </remarks>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Wyświetlana nazwa paczki (np. "D&D Core Rulebook").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Autor paczki (np. "DungeonApp Team").
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Wersja paczki (np. "1.0.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Opis paczki widoczny w menedżerze paczek.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Globalny rejestr szablonów właściwości (np. jak formatować i nazywać "Damage").
    /// </summary>
    public Dictionary<string, PropertyTemplate> PropertyTemplates { get; set; } = new();

    /// <summary>
    /// Globalny rejestr rzadkości przedmiotów zdefiniowanych przez paczkę.
    /// </summary>
    public Dictionary<string, RarityTemplate> Rarities { get; set; } = new();

    /// <summary>
    /// Słownik szablonów kategorii (np. core_type_weapon) zdefiniowanych w paczce.
    /// Definiuje m.in. jak właściwości tych przedmiotów mają być formatowane w UI.
    /// </summary>
    public Dictionary<string, CategoryTemplate> Categories { get; set; } = new();

    /// <summary>
    /// Słownik frakcji zdefiniowanych w paczce.
    /// </summary>
    public Dictionary<string, FactionTemplate> Factions { get; set; } = new();

    /// <summary>
    /// Słownik ikon wektorowych (np. "sword" -> "M10,10 L20...").
    /// Pozwala modderom definiować i udostępniać własne ikony SVG w pliku pack.json.
    /// </summary>
    public Dictionary<string, string> Icons { get; set; } = new();

    /// <summary>
    /// Lista szablonów przedmiotów zdefiniowanych w tej paczce.
    /// </summary>
    [JsonIgnore]
    public List<ItemTemplate> Items { get; set; } = new();

    /// <summary>
    /// Lista szablonów przeciwników zdefiniowanych w tej paczce.
    /// </summary>
    [JsonIgnore]
    public List<AdversaryTemplate> Adversaries { get; set; } = new();

    /// <summary>
    /// Lista scenariuszy fabularnych zdefiniowanych w tej paczce.
    /// </summary>
    [JsonIgnore]
    public List<AdventureTemplate> Adventures { get; set; } = new();
}
