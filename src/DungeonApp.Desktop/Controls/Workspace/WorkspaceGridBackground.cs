using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// Renders the campaign desk grid from <see cref="WorkspaceGridSettings.CellSize"/>. Keeping the
/// drawing in code lets it share the exact same configuration as panel snapping.
/// </summary>
public sealed class WorkspaceGridBackground : Control
{
    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<WorkspaceGridBackground, IBrush?>(nameof(GridLineBrush));

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (GridLineBrush is null)
        {
            return;
        }

        var pen = new Pen(GridLineBrush, 1);
        var width = Bounds.Width;
        var height = Bounds.Height;

        for (var x = 0d; x <= width; x += WorkspaceGridSettings.CellSize)
        {
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var y = 0d; y <= height; y += WorkspaceGridSettings.CellSize)
        {
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }
}
