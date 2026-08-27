using System;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// Which edges of a panel a resize gesture is dragging. Combined values describe corners.
/// <see cref="None"/> means the gesture moves the panel instead of resizing it.
/// </summary>
[Flags]
public enum PanelEdge
{
    None = 0,
    West = 1,
    East = 2,
    North = 4,
    South = 8,
    NorthWest = North | West,
    NorthEast = North | East,
    SouthWest = South | West,
    SouthEast = South | East
}
