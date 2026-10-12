using System;
using DungeonApp.Desktop.Workspace.Controls;

namespace DungeonApp.Desktop.Workspace.Panels;

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
    /// Singletons keep one instance keyed by <see cref="Id"/>. A multi-instance panel is told apart
    /// by its instance key, which the saved layout keeps.
    /// </summary>
    public bool AllowsMultipleInstances { get; init; }

    /// <summary>
    /// A window the GM opens and dismisses (opened at runtime, a cross in its header that removes
    /// it) instead of a tool that always lies on the desk and is only minimized. Such a window is
    /// never offered by the deck and is restored only if it was open.
    /// </summary>
    public bool ClosesPermanently { get; init; }

    /// <summary>
    /// False - the content does not usefully fill a whole desk (an entry card), so the window has no
    /// maximize button. A window with an upper size bound in either axis cannot be maximized either,
    /// whatever this says. A window whose width has Min == Max is fixed in width: only its height is
    /// changed by hand.
    /// </summary>
    public bool CanMaximize { get; init; } = true;

    /// <summary>Builds the body of the window with the given instance key; null - <see cref="CreateContent"/>.</summary>
    public Func<string, object>? CreateInstance { get; init; }

    /// <summary>Whether a saved window with the given instance key is still worth restoring; null - always.</summary>
    public Func<string, bool>? CanRestore { get; init; }

    /// <summary>The body of the window with the given instance key.</summary>
    public object CreateBody(string instanceKey) => CreateInstance?.Invoke(instanceKey) ?? CreateContent();
}
