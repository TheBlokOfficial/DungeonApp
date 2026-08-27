namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// A panel rectangle in logical (DIP) workspace coordinates. Deliberately independent of
/// Avalonia types so the whole geometry pipeline stays testable without a UI framework.
/// </summary>
public readonly record struct PanelPlacement(double X, double Y, double Width, double Height)
{
    public double Left => X;

    public double Top => Y;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    /// <summary>
    /// Builds a placement from edge coordinates. The result may have a negative extent when the
    /// edges are inverted mid-gesture; the constraint pass in <see cref="PanelGeometry"/> is what
    /// resolves that, so this stays a dumb conversion.
    /// </summary>
    public static PanelPlacement FromEdges(double left, double top, double right, double bottom) =>
        new(left, top, right - left, bottom - top);
}
