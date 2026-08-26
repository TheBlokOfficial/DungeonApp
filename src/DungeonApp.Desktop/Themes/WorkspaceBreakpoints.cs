namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Canonical workspace width thresholds from docs/ui/contract.md ("Breakpointy workspace'u").
/// Avalonia's `ContainerQuery.Query` is parsed by the XAML compiler from a literal string, so
/// these values cannot be referenced structurally from .axaml — keep any `min-width:` query in
/// sync with these constants by hand.
/// </summary>
public static class WorkspaceBreakpoints
{
    public const int StandardMinWidth = 960;
    public const int WideMinWidth = 1360;
}
