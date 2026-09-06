namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// Values are pinned because they are persisted as numbers (the layout store follows
/// workspace layout store, which does not register a string
/// enum converter). Inserting a member must never reinterpret an already saved layout.
/// </summary>
public enum PanelDisplayState
{
    Normal = 0,
    Minimized = 1,
    Maximized = 2
}
