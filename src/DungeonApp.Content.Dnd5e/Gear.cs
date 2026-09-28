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

    /// <summary>
    /// The item's picture: a PNG, JPEG or WebP file, as a path relative to this entry's pack. Shown
    /// square. Declared to the loader as this type's picture (<see cref="Dnd5eSystem"/>).
    /// </summary>
    public string? Image { get; init; }
}
