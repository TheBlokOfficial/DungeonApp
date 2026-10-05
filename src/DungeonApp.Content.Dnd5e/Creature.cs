using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e creature's values - a monster, an opponent or an NPC alike. This project is the only
/// place in the application allowed to know what a creature is; the shape below is a designed
/// record, not something assembled from data.
/// <para>
/// Named, not positional: some thirty properties of a handful of repeated types (<see cref="string"/>,
/// <see cref="int"/>) make adjacent positional parameters - <see cref="Senses"/> next to
/// <see cref="Languages"/>, <see cref="CombatAspect.Str"/> next to <see cref="CombatAspect.Dex"/> -
/// silently swappable past both the compiler and the deserializer. <see langword="required"/> marks
/// exactly the properties a creature entry cannot do without; <c>System.Text.Json</c> enforces that
/// on its own; the field-by-field <c>ValuesRejected</c> path never re-checks it.
/// </para>
/// <para>
/// The properties here are the type's own - read by the card and the content tab, by no tool. What a
/// fight reads (armor class, hit points, ability scores) is the <see cref="Combat"/> aspect, kept
/// under its own key. Every creature has it, as it has <see cref="Actions"/> and
/// <see cref="Challenge"/>: the card and the fight tool read them unconditionally, and a commoner is
/// a creature with a statblock of its own.
/// </para>
/// <para>
/// The six blocks of rules prose are <see cref="StatblockSection"/>s - named entries the pack author
/// wrote out, so the card can set each name apart without reading it out of the text. An entry that
/// holds a section as plain text fails deserialization and is rejected rather than misread.
/// </para>
/// </summary>
public sealed record Creature : IJsonOnDeserialized
{
    public required string Size { get; init; }

    public required string Type { get; init; }

    /// <summary>
    /// The category a "Stworzenia" content tab groups and filters this creature by. Optional and
    /// separate from <see cref="Type"/> on purpose - a pack author who leaves it out gets no
    /// category, never a silent fallback to <see cref="Type"/>; the content tab shows and filters a
    /// category-less entry correctly.
    /// </summary>
    public string? Group { get; init; }

    public required string Alignment { get; init; }

    /// <summary>What a fight reads.</summary>
    public required CombatAspect Combat { get; init; }

    public required string Speed { get; init; }

    public string? SavingThrows { get; init; }

    public string? Skills { get; init; }

    public string? DamageVulnerabilities { get; init; }

    public string? DamageResistances { get; init; }

    public string? DamageImmunities { get; init; }

    public string? ConditionImmunities { get; init; }

    public required string Senses { get; init; }

    public string? Languages { get; init; }

    /// <summary>
    /// The challenge rating exactly as written ("1/4", "5") - a label the card shows and the list
    /// orders by, never a number anything computes with. Experience points are <see cref="Xp"/>.
    /// </summary>
    public required string Challenge { get; init; }

    /// <summary>Experience points for defeating this creature, shown next to <see cref="Challenge"/>.</summary>
    public int? Xp { get; init; }

    public StatblockSection? SpecialAbilities { get; init; }

    /// <summary>
    /// The one section every creature has, with at least one entry (<see cref="OnDeserialized"/>).
    /// </summary>
    public required StatblockSection Actions { get; init; }

    /// <summary>
    /// The preamble (ability, save DC, attack bonus) as the introduction; each group of spells cast
    /// alike ("Bez ograniczeń", "1. poziom (4 komórki)") as an entry whose text lists the spells.
    /// </summary>
    public StatblockSection? Spellcasting { get; init; }

    public StatblockSection? BonusActions { get; init; }

    public StatblockSection? Reactions { get; init; }

    /// <summary>
    /// The per-turn allowance as the introduction, an action's cost ("kosztuje 2 akcje") as its
    /// entry's note - text the GM reads. Spending legendary actions belongs to the fight, not to
    /// the entry.
    /// </summary>
    public StatblockSection? LegendaryActions { get; init; }

    /// <summary>The creature's flavor, not its rules: plain text, no emphasis.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The creature's picture: a PNG, JPEG or WebP file, as a path relative to this entry's pack.
    /// Shown as an upright portrait. Declared to the loader as this type's picture
    /// (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public string? Image { get; init; }

    void IJsonOnDeserialized.OnDeserialized() => OnDeserialized();

    /// <summary>
    /// A creature with no action at all has nothing the GM can run at the table, so an
    /// <see cref="Actions"/> section with an introduction alone is refused like a missing one. The
    /// callback runs before the missing-required-property check, so it must tolerate an unset
    /// section and leave that refusal to the deserializer, which names the key.
    /// </summary>
    private void OnDeserialized()
    {
        if (Actions?.Entries.Count == 0)
        {
            throw new JsonException("\"actions\" needs at least one entry.");
        }
    }
}
