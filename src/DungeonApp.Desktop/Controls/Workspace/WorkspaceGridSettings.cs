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
    /// Normal panel placement follows the visible cell grid. Holding the precision modifier during
    /// a gesture temporarily selects the half-cell rhythm instead.
    /// </summary>
    public const WorkspaceGridSnapMode DefaultSnapMode = WorkspaceGridSnapMode.FullCell;

    public const WorkspaceGridSnapMode PreciseSnapMode = WorkspaceGridSnapMode.HalfCell;

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
    /// Minimalny rozmiar panelu licznika, przy którym karta wartości, przyciski i komunikat nie
    /// ściskają się wzajemnie.
    /// </summary>
    public const double CounterPanelMinWidth = CellSize * 8;

    public const double CounterPanelMinHeight = CellSize * 6;

    /// <summary>
    /// Maksymalny rozmiar panelu licznika, wyrażony w widocznych komórkach siatki, żeby okno
    /// pozostawało wyrównane do blatu zamiast rozrastać się w pustą powierzchnię.
    /// </summary>
    public const double CounterPanelMaxWidth = CellSize * 12;

    public const double CounterPanelMaxHeight = CellSize * 8;

    /// <summary>
    /// Panels may touch the surface boundary. Clamping still prevents them from leaving it.
    /// </summary>
    public const double EdgeMargin = 0;
}
