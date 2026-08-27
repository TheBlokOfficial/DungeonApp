using System;
using DungeonApp.Desktop.Controls.Workspace;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// Which desk group a panel is offered under. Mirrors the campaign navigation groups, so a module
/// still cannot invent its own place in the information architecture - it only picks one of these.
/// </summary>
public enum WorkspacePanelGroup
{
    Session,
    World,
    Knowledge,
    Campaign
}

/// <summary>
/// Everything the workspace needs to offer, open and restore one kind of panel. This is the
/// module-to-UI contract: the deck, the default layout and (later) the command palette are all
/// built from the same descriptors, so a panel cannot exist in one of them and be missing from
/// another.
/// <para>
/// Deliberately carries no domain knowledge. <see cref="CreateContent"/> returns an opaque view
/// model that the application's data templates resolve to a view.
/// </para>
/// </summary>
public sealed record WorkspacePanelDescriptor(
    string Id,
    string Title,
    string IconResourceKey,
    WorkspacePanelGroup Group,
    PanelPlacement DefaultPlacement,
    PanelConstraints Constraints,
    Func<object> CreateContent)
{
    /// <summary>
    /// Singletons keep one instance keyed by <see cref="Id"/>. Multi-instance panels are already
    /// representable in the saved layout through the instance key, but no UI opens a second one yet.
    /// </summary>
    public bool AllowsMultipleInstances { get; init; }
}
