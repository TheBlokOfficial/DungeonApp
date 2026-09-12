namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e gear item's values. Its card, like a monster's, is designed rather than assembled
/// from data. Named properties for the same reason as <see cref="Monster"/> - see its remarks.
/// </summary>
public sealed record Gear
{
    public required string Rarity { get; init; }

    public int? Weight { get; init; }

    public string? Description { get; init; }
}
