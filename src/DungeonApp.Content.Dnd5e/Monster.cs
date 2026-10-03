using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e monster's values. This project is the only place in the application allowed to know
/// what a monster is; the shape below is a designed record, not something assembled from data.
/// <para>
/// Named, not positional: some thirty properties of a handful of repeated types (<see cref="string"/>,
/// <see cref="int"/>) make adjacent positional parameters - <see cref="Str"/> next to
/// <see cref="Dex"/>, <see cref="Senses"/> next to <see cref="Languages"/> - silently swappable past
/// both the compiler and the deserializer. <see langword="required"/> marks exactly the properties
/// a monster entry cannot do without; <c>System.Text.Json</c> enforces that on its own; the
/// field-by-field <c>ValuesRejected</c> path never re-checks it.
/// </para>
/// <para>
/// The six blocks of rules prose are <see cref="StatblockSection"/>s - named entries the pack author
/// wrote out, so the card can set each name apart without reading it out of the text. An entry that
/// still holds a section as plain text fails deserialization and is rejected rather than misread.
/// </para>
/// </summary>
public sealed record Monster : IJsonOnDeserialized
{
    public required string Size { get; init; }

    public required string Type { get; init; }

    /// <summary>
    /// The category a "Potwory" content tab groups and filters this monster by. Optional and
    /// separate from <see cref="Type"/> on purpose - a pack author who leaves it out gets no
    /// category, never a silent fallback to <see cref="Type"/>; the content tab shows and filters a
    /// category-less entry correctly (it is how every "Przedmioty" entry behaves, since <c>Gear</c>
    /// declares no category at all).
    /// </summary>
    public string? Group { get; init; }

    public required string Alignment { get; init; }

    public required int Ac { get; init; }

    public string? AcSource { get; init; }

    public required int Hp { get; init; }

    public string? HpDice { get; init; }

    /// <summary>
    /// Current hit points for one specimen of this monster. An entry only ever declares the
    /// maximum in <see cref="Hp"/>; this is filled in by an instance's overlay, so it stays null
    /// until something GM-specific happens to a particular goblin.
    /// </summary>
    public int? CurrentHp { get; init; }

    public required string Speed { get; init; }

    public required int Str { get; init; }

    public required int Dex { get; init; }

    public required int Con { get; init; }

    public required int Int { get; init; }

    public required int Wis { get; init; }

    public required int Cha { get; init; }

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

    /// <summary>Experience points for defeating this monster, shown next to <see cref="Challenge"/>.</summary>
    public int? Xp { get; init; }

    public StatblockSection? SpecialAbilities { get; init; }

    /// <summary>The one section every monster has: at least one entry (<see cref="OnDeserialized"/>).</summary>
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
    /// entry's note - text the GM reads, never a counter anything spends.
    /// </summary>
    public StatblockSection? LegendaryActions { get; init; }

    /// <summary>The monster's flavor, not its rules: plain text, no emphasis.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The monster's picture: a PNG, JPEG or WebP file, as a path relative to this entry's pack.
    /// Shown as an upright portrait. Declared to the loader as this type's picture
    /// (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public string? Image { get; init; }

    void IJsonOnDeserialized.OnDeserialized() => OnDeserialized();

    /// <summary>
    /// A monster with no action at all has nothing the GM can run at the table, so an empty
    /// <see cref="Actions"/> section - an introduction alone - is refused like a missing one.
    /// </summary>
    private void OnDeserialized()
    {
        if (Actions is null || Actions.Entries.Count == 0)
        {
            throw new JsonException("\"actions\" needs at least one entry.");
        }
    }
}
