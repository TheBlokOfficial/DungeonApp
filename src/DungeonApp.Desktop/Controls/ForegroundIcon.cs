using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Draws an outline icon from the icon set (a <see cref="DrawingImage"/> in Themes/Icons.axaml) in the
/// foreground colour it inherits, not in the colour its pens were declared with. Inside a button the
/// icon therefore always has exactly the colour of the button's text - in every variant and state -
/// and fades with the content when the button fades it. Only the geometry, stroke thickness, caps and
/// joins come from the source drawing.
/// </summary>
public sealed class ForegroundIcon : Control
{
    public static readonly StyledProperty<DrawingImage?> SourceProperty =
        AvaloniaProperty.Register<ForegroundIcon, DrawingImage?>(nameof(Source));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ForegroundIcon>();

    static ForegroundIcon()
    {
        AffectsRender<ForegroundIcon>(SourceProperty, ForegroundProperty);
    }

    public DrawingImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var source = Source;
        var foreground = Foreground;
        if (source?.Drawing is not { } drawing || foreground is null)
        {
            return;
        }

        var viewbox = source.Viewbox ?? drawing.GetBounds();
        if (viewbox.Width <= 0 || viewbox.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(Bounds.Width / viewbox.Width, Bounds.Height / viewbox.Height);
        var offsetX = (Bounds.Width - viewbox.Width * scale) / 2;
        var offsetY = (Bounds.Height - viewbox.Height * scale) / 2;
        var transform = Matrix.CreateTranslation(-viewbox.X, -viewbox.Y)
                        * Matrix.CreateScale(scale, scale)
                        * Matrix.CreateTranslation(offsetX, offsetY);

        using (context.PushTransform(transform))
        {
            DrawInForeground(context, drawing, foreground);
        }
    }

    private static void DrawInForeground(DrawingContext context, Drawing drawing, IBrush foreground)
    {
        switch (drawing)
        {
            case DrawingGroup group:
                foreach (var child in group.Children)
                {
                    DrawInForeground(context, child, foreground);
                }

                break;

            case GeometryDrawing { Geometry: { } geometry } shape:
                var fill = PaintsNothing(shape.Brush) ? null : foreground;
                var pen = shape.Pen is null
                    ? null
                    : new Pen(foreground, shape.Pen.Thickness, shape.Pen.DashStyle, shape.Pen.LineCap,
                              shape.Pen.LineJoin, shape.Pen.MiterLimit);
                context.DrawGeometry(fill, pen, geometry);
                break;
        }
    }

    /// <summary>
    /// An outline shape in the set declares no Brush, but GeometryDrawing's default Brush is
    /// Transparent, not null - so "has a brush" alone would fill every outline icon solid. A shape is
    /// filled only when its source brush actually paints something.
    /// </summary>
    private static bool PaintsNothing(IBrush? brush) =>
        brush is null
        || brush.Opacity <= 0
        || brush is ISolidColorBrush { Color.A: 0 };
}
