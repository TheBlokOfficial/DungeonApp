using System.Collections.Generic;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon przeciwnika zdefiniowany w Paczce Zawartości (Content Pack).
/// </summary>
public class AdversaryTemplate
{
    /// <summary>
    /// Unikalny identyfikator przeciwnika w obrębie paczki (np. "goblin").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Opcjonalne referencyjne ID do innego przeciwnika (np. "core:goblin").
    /// </summary>
    public string? BaseTemplate { get; set; }

    /// <summary>
    /// Wyświetlana nazwa przeciwnika (np. "Goblin").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Opis fabularny.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Kategoria (Typ), np. "Beast", "Humanoid".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Rozmiar, np. "Medium", "Large".
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Charakter, np. "Chaotic Evil".
    /// </summary>
    public string Alignment { get; set; } = string.Empty;

    /// <summary>
    /// Przynależność organizacyjna/frakcyjna (np. "core_faction_undead_scourge").
    /// </summary>
    public string Faction { get; set; } = string.Empty;

    /// <summary>
    /// Poziom trudności (CR), w formacie tekstowym np. "1/4", "10".
    /// </summary>
    public string ChallengeRating { get; set; } = string.Empty;

    /// <summary>
    /// Tagi opisujące naturę przeciwnika.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Statystyki bitewne przeciwnika (HP, AC, Speed).
    /// </summary>
    public DungeonApp.Models.ContentPacks.Components.CombatStats Combat { get; set; } = new();

    /// <summary>
    /// Szybkość przeciwnika (lista różnych typów ruchu, np. Walk, Fly).
    /// </summary>
    public List<DungeonApp.Models.ContentPacks.Components.SpeedComponent> Speeds { get; set; } = new();

    /// <summary>
    /// Atrybuty przeciwnika (Siła, Zręczność, itp.).
    /// </summary>
    public DungeonApp.Models.ContentPacks.Components.AbilityScores Abilities { get; set; } = new();

    /// <summary>
    /// Akcje i ataki dostępne dla przeciwnika.
    /// </summary>
    public List<DungeonApp.Models.ContentPacks.Components.ActionDefinition> Actions { get; set; } = new();
}
