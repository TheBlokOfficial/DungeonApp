using System.Collections.Generic;
using DungeonApp.Desktop.Controls.Workspace;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

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
/// A whole desk arrangement. Closed panels stay in the list so reopening one from the deck puts it
/// back where it last was.
/// </summary>
public sealed record WorkspaceLayout(
    int Version,
    double SurfaceWidth,
    double SurfaceHeight,
    IReadOnlyList<WorkspacePanelLayout> Panels)
{
    public const int CurrentVersion = 1;

    /// <summary>Upper bound on remembered entries, so a long-lived file cannot grow without limit.</summary>
    public const int MaxPanels = 64;

    /// <summary>Returned for a missing, unreadable or future-versioned file. Means "use the defaults".</summary>
    public static WorkspaceLayout Empty { get; } = new(CurrentVersion, 0, 0, []);

    public bool IsEmpty => Panels.Count == 0;
}
