namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>Supported snap densities inside one visible workspace-grid cell.</summary>
public enum WorkspaceGridSnapMode
{
    FullCell = 1,
    HalfCell = 2,
    QuarterCell = 4
}

/// <summary>
/// Single source of truth for the campaign desk grid. Both the rendered background and panel
/// geometry consume these values, so changing the visual cell cannot silently desynchronise snap.
/// </summary>
public static class WorkspaceGridSettings
{
    /// <summary>Size of one visible background-grid cell, in logical Avalonia units (DIP).</summary>
    public const double CellSize = 32;

    /// <summary>
    /// Selected snap density. This is the only switch needed to move between full-, half-, and
    /// quarter-cell snapping.
    /// </summary>
    public const WorkspaceGridSnapMode SnapMode = WorkspaceGridSnapMode.QuarterCell;

    /// <summary>Effective snap step: currently one quarter of a 32-DIP cell, or 8 DIP.</summary>
    public const double SnapStep = CellSize / (int)SnapMode;

    /// <summary>Distance at which an edge or another panel wins over ordinary grid snapping.</summary>
    public const double SnapRadius = SnapStep;

    /// <summary>Short enough to feel immediate, long enough to make the snap destination legible.</summary>
    public const double SnapAnimationDurationMilliseconds = 120;

    /// <summary>Default gap between neighbouring panels, aligned to one snap interval.</summary>
    public const double PanelGap = SnapStep;

    /// <summary>
    /// Panels may touch the surface boundary. Clamping still prevents them from leaving it.
    /// </summary>
    public const double EdgeMargin = 0;
}
