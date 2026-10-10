namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Where a value with a base stands against it: unchanged, raised or lowered by effects. The view
/// shows it as the colour of the digits (DungeonAboveBaseBrush, DungeonBelowBaseBrush); which effects
/// moved the value is the system's business.
/// </summary>
public enum BaseDeviation
{
    None,
    Above,
    Below,
}
