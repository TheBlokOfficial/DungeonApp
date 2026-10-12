using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Blurred backdrop of a glass surface: draws a snapshot of the window (<see cref="Source"/>, taken at the
/// window's origin) shifted so that each of its pixels lies exactly over the window pixel it was taken from, and
/// blurs it with <see cref="BlurRadius"/>. Avalonia cannot blur the live content under a control, so a surface
/// that keeps the content under it still (an overlay that captures input) shows a snapshot instead.
/// The position comes from layout alone, ignoring render transforms: a parent sliding in with a transform
/// cancels its own motion with an opposite transform on this control, so the backdrop stands still while the
/// surface's clip moves over it. Takes no space in layout; the parent clips it to the surface outline.
/// </summary>
public sealed class GlassBackdrop : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<GlassBackdrop, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<GlassBackdrop, double>(nameof(BlurRadius));

    static GlassBackdrop()
    {
        AffectsRender<GlassBackdrop>(SourceProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<GlassBackdrop>(false);
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    /// <summary>Extra snapshot drawn past each edge, so the blur near the edge mixes real content, not emptiness.</summary>
    private double Bleed => Math.Max(0, BlurRadius) * 2;

    public override void Render(DrawingContext context)
    {
        if (Source is not { } source || TopLevel.GetTopLevel(this) is not Visual root)
        {
            return;
        }

        var origin = LayoutOrigin(root);
        var bleed = Bleed;
        using (context.PushClip(new Rect(Bounds.Size).Inflate(bleed)))
        {
            context.DrawImage(source, new Rect(-origin.X, -origin.Y, source.Size.Width, source.Size.Height));
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BlurRadiusProperty)
        {
            Effect = BlurRadius > 0 ? new BlurEffect { Radius = BlurRadius } : null;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // A new position of the surface moves the snapshot under it; the drawing is not redone by itself.
        InvalidateVisual();
        return base.ArrangeOverride(finalSize);
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    private Point LayoutOrigin(Visual root)
    {
        var x = 0d;
        var y = 0d;
        for (Visual? visual = this; visual is not null && visual != root; visual = visual.GetVisualParent())
        {
            x += visual.Bounds.X;
            y += visual.Bounds.Y;
        }

        return new Point(x, y);
    }
}
