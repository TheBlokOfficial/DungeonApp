using Avalonia;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// Reads the scale-profile metrics the geometry pipeline needs out of application resources
/// (written by <see cref="DungeonApp.Desktop.Themes.UiScaleProfiles"/>).
/// <para>
/// Called once per gesture and once per surface resize — never per pointer move. The resolved
/// value is then passed down as a <see cref="WorkspaceMetrics"/> so the math itself never touches
/// the resource system.
/// </para>
/// </summary>
internal static class WorkspaceMetricsResolver
{
    public static WorkspaceMetrics Resolve()
    {
        var application = Application.Current;
        if (application is null)
        {
            return WorkspaceMetrics.Fallback;
        }

        return new WorkspaceMetrics(
            Padding: Read(application, "DungeonWorkspacePadding", WorkspaceMetrics.Fallback.Padding),
            Gap: Read(application, "DungeonPanelGap", WorkspaceMetrics.Fallback.Gap),
            MinPanelWidth: Read(application, "DungeonPanelMinWidth", WorkspaceMetrics.Fallback.MinPanelWidth),
            MinPanelHeight: Read(application, "DungeonPanelMinHeight", WorkspaceMetrics.Fallback.MinPanelHeight));
    }

    private static double Read(Application application, string key, double fallback) =>
        application.TryGetResource(key, application.ActualThemeVariant, out var resource) && resource is double value
            ? value
            : fallback;
}
