namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The charges aspect: how many charges an item holds and how they come back. Data without a look:
/// <see cref="GearCardView"/> shows <see cref="Max"/> as a headline value and <see cref="Recharge"/>
/// among the pairs. Optional on a <see cref="Gear"/>. Spending and regaining charges is the ledger's
/// job at the table, never the entry's.
/// </summary>
public sealed record ChargesAspect
{
    /// <summary>How many charges the item holds when full.</summary>
    public required int Max { get; init; }

    /// <summary>How the charges come back, as text the GM reads ("odzyskuje 1k6+1 ładunków o świcie").</summary>
    public string? Recharge { get; init; }
}
