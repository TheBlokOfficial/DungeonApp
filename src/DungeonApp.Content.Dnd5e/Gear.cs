using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e item's values. Its card, like a monster's, is designed rather than assembled from
/// data. Named properties for the same reason as <see cref="Monster"/> - see its remarks.
/// <para>
/// Every value is text the GM reads or a number the card formats; nothing here is ever computed
/// with. <see cref="Recharge"/> in particular is a line on the card, never a timer.
/// </para>
/// </summary>
public sealed record Gear : IJsonOnDeserialized
{
    /// <summary>
    /// One of this system's five rarity tiers, "Pospolity" up to "Legendarny" (<see cref="Dnd5eSystem"/>).
    /// Any other text is kept and shown as written, without a tier's color. Says how rare the item
    /// is, never whether it is magical - that is <see cref="Magical"/>.
    /// </summary>
    public required string Rarity { get; init; }

    /// <summary>Whether the item is magical - it matters to the rules, so the card shows it as a pill.</summary>
    public bool Magical { get; init; }

    /// <summary>
    /// What kind of thing the item is ("Broń", "Zbroja", "Mikstura") - free text; the content tab's
    /// category (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// The item's class within <see cref="Category"/> that matters to the rules ("żołnierska, do walki
    /// wręcz", "ciężka"), never a repeat of the item's name - it stands on the card as a pill.
    /// </summary>
    public string? Subtype { get; init; }

    public bool Attunement { get; init; }

    /// <summary>Who may attune to the item, as written after "wymagane" ("przez czarodzieja").</summary>
    public string? AttunementBy { get; init; }

    /// <summary>In kilograms; shown through <see cref="UnitScale.Weight"/>.</summary>
    public decimal? Weight { get; init; }

    /// <summary>
    /// The item's worth on an abstract, relative scale - a number with no unit, never negative, its
    /// scale not bounded either way. Not a price: a price belongs to a particular slot in an
    /// inventory or a shop.
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>A weapon's damage dice as written ("1k8") - the card's large value.</summary>
    public string? Damage { get; init; }

    /// <summary>The kind of <see cref="Damage"/> ("cięte"), written under it.</summary>
    public string? DamageType { get; init; }

    /// <summary>A weapon's properties as written ("uniwersalna (1k10)").</summary>
    public string? Properties { get; init; }

    /// <summary>
    /// Armor's or a shield's armor class, short ("16", "11", "+2") - the card's large value; what is
    /// added to it goes to <see cref="ArmorClassNote"/>.
    /// </summary>
    public string? ArmorClass { get; init; }

    /// <summary>What is added to <see cref="ArmorClass"/> ("+ mod. Zr (maks. 2)"), written under it.</summary>
    public string? ArmorClassNote { get; init; }

    /// <summary>The Strength score heavy armor asks for.</summary>
    public int? StrengthRequirement { get; init; }

    public bool StealthDisadvantage { get; init; }

    public int? Charges { get; init; }

    /// <summary>How the charges come back, as text the GM reads ("odzyskuje 1k6+1 ładunków o świcie").</summary>
    public string? Recharge { get; init; }

    /// <summary>The item's flavor and rules prose: plain text, no emphasis.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The item's picture: a PNG, JPEG or WebP file, as a path relative to this entry's pack. Shown
    /// square. Declared to the loader as this type's picture (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public string? Image { get; init; }

    void IJsonOnDeserialized.OnDeserialized() => OnDeserialized();

    /// <summary>
    /// Saying who may attune to an item that needs no attunement contradicts itself; the entry is
    /// refused rather than drawn with one of the two silently ignored. So is a note under a value that
    /// is not there (a damage type without dice, an armor class note without an armor class). A
    /// negative worth means nothing on the scale and is refused too.
    /// </summary>
    private void OnDeserialized()
    {
        if (Value < 0)
        {
            throw new JsonException("\"value\" cannot be negative.");
        }

        if (AttunementBy is not null && !Attunement)
        {
            throw new JsonException("\"attunementBy\" needs \"attunement\": true.");
        }

        if (DamageType is not null && Damage is null)
        {
            throw new JsonException("\"damageType\" needs \"damage\".");
        }

        if (ArmorClassNote is not null && ArmorClass is null)
        {
            throw new JsonException("\"armorClassNote\" needs \"armorClass\".");
        }
    }
}
