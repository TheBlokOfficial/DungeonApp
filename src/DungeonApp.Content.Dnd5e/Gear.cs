using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e item's values. Its card, like a creature's, is designed rather than assembled from
/// data. Named properties for the same reason as <see cref="Creature"/> - see its remarks.
/// <para>
/// The properties here are the type's own, read by the card and the content tab. What carrying and
/// trading read is the required <see cref="Item"/> aspect; what the ledger counts is the optional
/// <see cref="Charges"/> aspect. An armor's <see cref="ArmorClass"/> - what it gives its wearer - is
/// the item's own field, not a fight's armor class.
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

    /// <summary>The item's weight and worth - every item has both.</summary>
    public required ItemAspect Item { get; init; }

    /// <summary>A weapon's damage dice as written ("1k8") - the card's large value.</summary>
    public string? Damage { get; init; }

    /// <summary>The kind of <see cref="Damage"/> ("cięte"), written under it.</summary>
    public string? DamageType { get; init; }

    /// <summary>A weapon's properties as written ("półtoraręczna (1k10)").</summary>
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

    /// <summary>The item's charges; null for an item that holds none.</summary>
    public ChargesAspect? Charges { get; init; }

    /// <summary>
    /// The item's flavor and rules prose, shown as the card's "Opis" section; emphasis marked by the
    /// pack as in a creature's sections (<c>**…**</c>).
    /// </summary>
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
    /// is not there (a damage type without dice, an armor class note without an armor class).
    /// </summary>
    private void OnDeserialized()
    {
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
