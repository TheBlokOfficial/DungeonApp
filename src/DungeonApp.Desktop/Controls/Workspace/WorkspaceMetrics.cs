namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// Scale-profile dependent measurements the geometry pipeline needs. Passed in as a value rather
/// than read from <c>Application.Resources</c>, because these are consumed on every pointer move
/// and a resource lookup per frame would be wasteful.
/// </summary>
public readonly record struct WorkspaceMetrics(
    double EdgeMargin,
    double Gap,
    double MinPanelWidth,
    double MinPanelHeight)
{
    /// <summary>Matches the Medium profile; used before the surface has resolved its resources.</summary>
    public static WorkspaceMetrics Fallback { get; } = new(
        WorkspaceGridSettings.EdgeMargin,
        WorkspaceGridSettings.PanelGap,
        240,
        152);
}
