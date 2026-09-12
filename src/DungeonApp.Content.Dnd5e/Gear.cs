namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// A D&amp;D 5e gear item's values. Its card, like a monster's, is designed rather than assembled
/// from data.
/// </summary>
public sealed record Gear(
    string Rarity,
    int? Weight,
    string? Description);
