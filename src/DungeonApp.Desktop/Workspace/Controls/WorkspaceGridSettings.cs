namespace DungeonApp.Desktop.Workspace.Controls;

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
    /// Normal panel placement follows the visible cell grid. Holding the precision modifier during
    /// a gesture temporarily selects the half-cell rhythm instead.
    /// </summary>
    public const WorkspaceGridSnapMode DefaultSnapMode = WorkspaceGridSnapMode.HalfCell;

    public const WorkspaceGridSnapMode PreciseSnapMode = WorkspaceGridSnapMode.QuarterCell;

    /// <summary>Normal snap step: one 32-DIP cell.</summary>
    public const double DefaultSnapStep = CellSize / (int)DefaultSnapMode;

    /// <summary>Precision-modifier snap step: one half of a 32-DIP cell, or 16 DIP.</summary>
    public const double PreciseSnapStep = CellSize / (int)PreciseSnapMode;

    /// <summary>
    /// Distance at which an edge or another panel wins over ordinary grid snapping. It deliberately
    /// stays independent of the normal grid step so coarser placement does not feel more magnetic.
    /// </summary>
    public const double SnapRadius = PreciseSnapStep;

    /// <summary>Short enough to feel immediate, long enough to make the snap destination legible.</summary>
    public const double SnapAnimationDurationMilliseconds = 120;

    /// <summary>Default gap between neighbouring panels, aligned to the precise snap interval.</summary>
    public const double PanelGap = PreciseSnapStep;

    /// <summary>
    /// Minimum size of a desk-tool window that keeps its content from collapsing in on
    /// itself.
    /// </summary>
    public const double ToolPanelMinWidth = CellSize * 8;

    public const double ToolPanelMinHeight = CellSize * 6;

    /// <summary>
    /// Maximum size of a desk-tool window, expressed in visible grid cells so that the window
    /// stays aligned with the desk rather than expanding into empty space.
    /// </summary>
    public const double ToolPanelMaxWidth = CellSize * 12;

    public const double ToolPanelMaxHeight = CellSize * 8;

    /// <summary>Width range of the world catalog lying on the desk, in whole grid cells.</summary>
    public const double CatalogDefaultWidth = CellSize * 9;

    public const double CatalogMinWidth = CellSize * 6;

    public const double CatalogMaxWidth = CellSize * 16;

    /// <summary>Gap between the catalog's default place and the desk's top and right edges.</summary>
    public const double CatalogEdgeInset = CellSize;

    /// <summary>
    /// Panels may touch the surface boundary. Clamping still prevents them from leaving it.
    /// </summary>
    public const double EdgeMargin = 0;
}
