using System.Collections.Generic;
using DungeonApp.Desktop.Workspace.Controls;

namespace DungeonApp.Desktop.Workspace.Layout;

/// <summary>
/// One saved panel. The geometry is the panel's <em>desired</em> placement in logical (DIP) units,
/// never the clamped effective one - that is what lets a layout authored on a wide monitor come
/// back intact on a narrow one once the window grows again.
/// </summary>
public sealed record WorkspacePanelLayout(
    string DescriptorId,
    string InstanceKey,
    bool IsOpen,
    PanelDisplayState State,
    int ZOrder,
    double X,
    double Y,
    double Width,
    double Height);

/// <summary>
/// The world catalog lying on the desk. <see cref="X"/> and <see cref="Y"/> are the desired position of
/// its top-left corner, null while the GM has not moved it (the default is the desk's top-right
/// corner, which depends on the desk's size). <see cref="ExpandedFolders"/> holds folder ids as text.
/// </summary>
public sealed record WorldCatalogLayout(
    double? X,
    double? Y,
    double Width,
    bool IsRootCollapsed,
    IReadOnlyList<string> ExpandedFolders);

/// <summary>
/// A whole desk arrangement. <see cref="WorkspacePanelLayout.IsOpen"/> remains in schema v1 only to
/// migrate older closed modules to the minimized state. A file without a <see cref="Catalog"/> (null)
/// restores the catalog's defaults.
/// </summary>
public sealed record WorkspaceLayout(
    int Version,
    double SurfaceWidth,
    double SurfaceHeight,
    IReadOnlyList<WorkspacePanelLayout> Panels,
    WorldCatalogLayout? Catalog = null)
{
    public const int CurrentVersion = 1;

    /// <summary>Upper bound on remembered entries, so a long-lived file cannot grow without limit.</summary>
    public const int MaxPanels = 64;

    /// <summary>Upper bound on remembered expanded folders, for the same reason.</summary>
    public const int MaxExpandedFolders = 512;

    /// <summary>Returned for a missing, unreadable or future-versioned file. Means "use the defaults".</summary>
    public static WorkspaceLayout Empty { get; } = new(CurrentVersion, 0, 0, []);

    public bool IsEmpty => Panels.Count == 0;
}
