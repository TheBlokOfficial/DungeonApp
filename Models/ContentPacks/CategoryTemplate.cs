using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Szablon kategorii (np. core_type_weapon) określający domyślne reguły wyświetlania dla przedmiotów danego typu.
/// </summary>
public class CategoryTemplate
{
    /// <summary>
    /// Unikalny identyfikator kategorii (np. "core_type_weapon").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa wyświetlana (może być kluczem tłumaczenia).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lista szablonów formatowania właściwości (np. ikona miecza i tekst obrażeń),
    /// które będą renderowane w tabeli rejestru dla wszystkich przedmiotów tej kategorii.
    /// </summary>
    public List<PropertyBadgeTemplate> RegistryFormat { get; set; } = new();
}
