using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// The mark of an uncommitted change: a soft blot of overlapping ovals painted behind the decorated
/// content while <see cref="IsShown"/> is set. It never changes the content's colour or background
/// and never takes room - it is drawn around the child's box, reaching past it, so a value gains or
/// loses the blot without anything moving. The colour belongs to the frame's control theme
/// (Themes/Controls/ChangeBlot.axaml).
/// </summary>
public sealed class ChangeBlot : Decorator
{
    public static readonly StyledProperty<bool> IsShownProperty =
        AvaloniaProperty.Register<ChangeBlot, bool>(nameof(IsShown));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ChangeBlot, IBrush?>(nameof(Fill));

    static ChangeBlot()
    {
        AffectsRender<ChangeBlot>(IsShownProperty, FillProperty);
    }

    public bool IsShown
    {
        get => GetValue(IsShownProperty);
        set => SetValue(IsShownProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (IsShown && Fill is { } fill && Bounds.Width > 0 && Bounds.Height > 0)
        {
            context.DrawGeometry(fill, null, Shape(new Rect(Bounds.Size)));
        }
    }

    /// <summary>
    /// A few tilted ovals around the box, sized by its height so a one-digit and a three-digit value
    /// get blots of the same weight. One geometry filled once (non-zero rule), so where the ovals overlap
    /// the translucent colour does not darken.
    /// </summary>
    private static Geometry Shape(Rect box)
    {
        var h = box.Height;
        var w = box.Width;
        var cy = box.Y + h * 0.55;
        var group = new GeometryGroup { FillRule = FillRule.NonZero };
        group.Children.Add(Oval(box.X + w * 0.5, cy, w * 0.5 + h * 0.24, h * 0.32, -8));
        group.Children.Add(Oval(box.X + w * 0.1, cy + h * 0.06, h * 0.3, h * 0.29, 0));
        group.Children.Add(Oval(box.X + w * 0.9, cy - h * 0.07, h * 0.31, h * 0.26, -25));
        group.Children.Add(Oval(box.X + w * 0.42, cy - h * 0.18, w * 0.3 + h * 0.16, h * 0.23, 10));
        group.Children.Add(Oval(box.X + w * 0.62, cy + h * 0.19, w * 0.26 + h * 0.14, h * 0.2, -12));
        return group;
    }

    private static EllipseGeometry Oval(double centerX, double centerY, double radiusX, double radiusY, double angle) =>
        new()
        {
            Center = new Point(centerX, centerY),
            RadiusX = Math.Max(0, radiusX),
            RadiusY = Math.Max(0, radiusY),
            Transform = new RotateTransform(angle, centerX, centerY),
        };
}
