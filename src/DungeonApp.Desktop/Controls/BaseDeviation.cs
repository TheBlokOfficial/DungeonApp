using DungeonApp.Core.Entries.Entities;

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

/// <summary>Turns the frame's sum of a value with a base into the deviation a view colours.</summary>
public static class BaseDeviations
{
    public static BaseDeviation Of(ShownValue value) => value.Offset switch
    {
        > 0 => BaseDeviation.Above,
        < 0 => BaseDeviation.Below,
        _ => BaseDeviation.None,
    };
}
