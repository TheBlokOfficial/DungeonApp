namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e monster's values. This project is the only place in the application allowed to know
/// what a monster is; the shape below is a designed record, not something assembled from data.
/// <para>
/// Named, not positional: twenty-one properties of a handful of repeated types (<see cref="string"/>,
/// <see cref="int"/>) make adjacent positional parameters - <see cref="Str"/> next to
/// <see cref="Dex"/>, <see cref="Senses"/> next to <see cref="Languages"/> - silently swappable past
/// both the compiler and the deserializer. <see langword="required"/> marks exactly the properties
/// <c>tests/DungeonApp.Core.Tests/Packs/dnd5e/templates/monster.json</c> declares without
/// <c>"required": false"</c>; <c>System.Text.Json</c> enforces that on its own; the field-by-field
/// <c>ValuesRejected</c> path never re-checks it.
/// </para>
/// </summary>
public sealed record Monster
{
    public required string Size { get; init; }

    public required string Type { get; init; }

    public required string Alignment { get; init; }

    public required int Ac { get; init; }

    public string? AcSource { get; init; }

    public required int Hp { get; init; }

    public string? HpDice { get; init; }

    public required string Speed { get; init; }

    public required int Str { get; init; }

    public required int Dex { get; init; }

    public required int Con { get; init; }

    public required int Int { get; init; }

    public required int Wis { get; init; }

    public required int Cha { get; init; }

    public string? Skills { get; init; }

    public required string Senses { get; init; }

    public string? Languages { get; init; }

    public required string Challenge { get; init; }

    public string? SpecialAbilities { get; init; }

    public required string Actions { get; init; }

    public string? Description { get; init; }
}
