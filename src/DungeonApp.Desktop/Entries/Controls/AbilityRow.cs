namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// One row an <see cref="AbilityTableView"/> shows: a label, a score and the score's own already
/// formatted modifier ("+2", "−1") - plain strings the card view supplies, exactly like
/// <see cref="TraitRow"/>. <see cref="ModifierTone"/> says which way the modifier counts; the card's
/// system decides that, the table only tints the cell for it.
/// </summary>
public sealed record AbilityRow(string Label, string Score, string Modifier, ValueTone ModifierTone = ValueTone.Neutral);

/// <summary>
/// Which way a number counts, for a cell that tints by it: adds (<see cref="Positive"/>), takes away
/// (<see cref="Negative"/>) or neither. The frame owns only this direction and its two tints
/// (DungeonPositiveValueDimBrush, DungeonNegativeValueDimBrush); what makes a value positive is the
/// system's arithmetic.
/// </summary>
public enum ValueTone
{
    Neutral,
    Positive,
    Negative,
}
