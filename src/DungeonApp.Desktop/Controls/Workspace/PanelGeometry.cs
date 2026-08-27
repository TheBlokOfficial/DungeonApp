using System;
using System.Collections.Generic;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// All non-trivial workspace arithmetic, deliberately free of Avalonia types so it can be unit
/// tested without a UI framework.
/// <para>
/// Nothing here may be moved into a ViewModel property setter. A setter that snaps or clamps turns
/// the container's TwoWay binding into an oscillator: the view writes 103.7, the setter stores 104,
/// the binding pushes 104 back, the control still believes 103.7, and so on. Placement setters are
/// dumb stores; this class is called before they are written.
/// </para>
/// </summary>
public static class PanelGeometry
{
    /// <summary>The UI contract's base grid unit. Also device-pixel safe at 100/125/150/200% scaling.</summary>
    public const double Grid = 4;

    /// <summary>How close an edge must come to a snap candidate before it is captured.</summary>
    public const double SnapRadius = 8;

    public static double SnapToGrid(double value) => Math.Round(value / Grid) * Grid;

    /// <summary>
    /// Moves a panel. Snapping is per axis and independent: a candidate within
    /// <see cref="SnapRadius"/> beats the grid, otherwise the value falls back to the grid.
    /// </summary>
    public static PanelPlacement SnapMove(
        PanelPlacement raw,
        double surfaceWidth,
        double surfaceHeight,
        IReadOnlyList<PanelPlacement> peers,
        WorkspaceMetrics metrics)
    {
        var x = SnapMoveAxis(raw.X, raw.Width, surfaceWidth, peers, metrics, horizontal: true);
        var y = SnapMoveAxis(raw.Y, raw.Height, surfaceHeight, peers, metrics, horizontal: false);

        // Math.Max guards the inversion that happens when the panel is larger than the surface:
        // without it the clamp range is reversed and Math.Clamp throws.
        x = Math.Clamp(x, metrics.Padding, Math.Max(metrics.Padding, surfaceWidth - metrics.Padding - raw.Width));
        y = Math.Clamp(y, metrics.Padding, Math.Max(metrics.Padding, surfaceHeight - metrics.Padding - raw.Height));

        return new PanelPlacement(x, y, raw.Width, raw.Height);
    }

    /// <summary>
    /// Applies a total (not per-frame) pointer delta to the dragged edges only. The result may be
    /// inverted; <see cref="SnapResize"/> resolves that.
    /// </summary>
    public static PanelPlacement ResizeRaw(PanelPlacement press, PanelEdge edge, double deltaX, double deltaY)
    {
        var left = press.Left + (edge.HasFlag(PanelEdge.West) ? deltaX : 0);
        var top = press.Top + (edge.HasFlag(PanelEdge.North) ? deltaY : 0);
        var right = press.Right + (edge.HasFlag(PanelEdge.East) ? deltaX : 0);
        var bottom = press.Bottom + (edge.HasFlag(PanelEdge.South) ? deltaY : 0);

        return PanelPlacement.FromEdges(left, top, right, bottom);
    }

    /// <summary>
    /// Snaps and constrains a resize. Order matters: snap, then min/max, then the surface, then
    /// min once more. Snapping after clamping would re-break the clamp.
    /// <para>
    /// Limits are enforced in edge space and always by moving the dragged edge back toward the
    /// anchor. Writing a Width below MinWidth instead would let the layout system silently clamp
    /// the measured size while the already-written position stands, so the anchored edge would
    /// creep sideways and the panel would walk across the surface.
    /// </para>
    /// </summary>
    public static PanelPlacement SnapResize(
        PanelPlacement raw,
        PanelEdge edge,
        double surfaceWidth,
        double surfaceHeight,
        IReadOnlyList<PanelPlacement> peers,
        PanelConstraints constraints,
        WorkspaceMetrics metrics)
    {
        double left = raw.Left, top = raw.Top, right = raw.Left + raw.Width, bottom = raw.Top + raw.Height;

        var draggingWest = edge.HasFlag(PanelEdge.West);
        var draggingEast = edge.HasFlag(PanelEdge.East);
        var draggingNorth = edge.HasFlag(PanelEdge.North);
        var draggingSouth = edge.HasFlag(PanelEdge.South);

        if (draggingWest)
        {
            left = SnapResizeAxis(left, surfaceWidth, peers, metrics, horizontal: true, leading: true);
        }

        if (draggingEast)
        {
            right = SnapResizeAxis(right, surfaceWidth, peers, metrics, horizontal: true, leading: false);
        }

        if (draggingNorth)
        {
            top = SnapResizeAxis(top, surfaceHeight, peers, metrics, horizontal: false, leading: true);
        }

        if (draggingSouth)
        {
            bottom = SnapResizeAxis(bottom, surfaceHeight, peers, metrics, horizontal: false, leading: false);
        }

        var width = Math.Clamp(right - left, constraints.MinWidth, constraints.EffectiveMaxWidth);
        if (draggingWest)
        {
            left = right - width;
        }
        else
        {
            right = left + width;
        }

        var height = Math.Clamp(bottom - top, constraints.MinHeight, constraints.EffectiveMaxHeight);
        if (draggingNorth)
        {
            top = bottom - height;
        }
        else
        {
            bottom = top + height;
        }

        if (draggingWest)
        {
            left = Math.Max(left, metrics.Padding);
        }

        if (draggingEast)
        {
            right = Math.Min(right, surfaceWidth - metrics.Padding);
        }

        if (draggingNorth)
        {
            top = Math.Max(top, metrics.Padding);
        }

        if (draggingSouth)
        {
            bottom = Math.Min(bottom, surfaceHeight - metrics.Padding);
        }

        // The surface clamp can violate the minimum when the surface is smaller than the panel's
        // floor. That is physically unavoidable, so here the anchor is the edge that gives way.
        if (right - left < constraints.MinWidth)
        {
            if (draggingWest)
            {
                left = right - constraints.MinWidth;
            }
            else
            {
                right = left + constraints.MinWidth;
            }
        }

        if (bottom - top < constraints.MinHeight)
        {
            if (draggingNorth)
            {
                top = bottom - constraints.MinHeight;
            }
            else
            {
                bottom = top + constraints.MinHeight;
            }
        }

        return PanelPlacement.FromEdges(left, top, right, bottom);
    }

    /// <summary>
    /// Projects a desired placement onto the space actually available. Never scales: it shrinks
    /// only when the panel genuinely cannot fit, and only down to the minimum, then translates the
    /// panel back inside.
    /// <para>
    /// Callers must leave the desired placement untouched. Recomputing this on every surface resize
    /// is what makes shrink-then-grow non-destructive: growing the window makes this the identity
    /// again and every panel returns exactly where the user put it.
    /// </para>
    /// </summary>
    public static PanelPlacement FitInto(
        PanelPlacement desired,
        double surfaceWidth,
        double surfaceHeight,
        PanelConstraints constraints,
        WorkspaceMetrics metrics)
    {
        var availableWidth = Math.Max(constraints.MinWidth, surfaceWidth - (2 * metrics.Padding));
        var availableHeight = Math.Max(constraints.MinHeight, surfaceHeight - (2 * metrics.Padding));

        var width = Math.Max(constraints.MinWidth, Math.Min(desired.Width, availableWidth));
        var height = Math.Max(constraints.MinHeight, Math.Min(desired.Height, availableHeight));

        var x = Math.Clamp(desired.X, metrics.Padding, Math.Max(metrics.Padding, surfaceWidth - metrics.Padding - width));
        var y = Math.Clamp(desired.Y, metrics.Padding, Math.Max(metrics.Padding, surfaceHeight - metrics.Padding - height));

        return new PanelPlacement(x, y, width, height);
    }

    /// <summary>
    /// The placement a maximized panel occupies. The desired placement is left alone and therefore
    /// doubles as the restore geometry, which is why no separate restore fields exist.
    /// </summary>
    public static PanelPlacement Maximize(double surfaceWidth, double surfaceHeight, WorkspaceMetrics metrics) =>
        new(
            metrics.Padding,
            metrics.Padding,
            Math.Max(metrics.MinPanelWidth, surfaceWidth - (2 * metrics.Padding)),
            Math.Max(metrics.MinPanelHeight, surfaceHeight - (2 * metrics.Padding)));

    private static double SnapMoveAxis(
        double value,
        double extent,
        double surfaceExtent,
        IReadOnlyList<PanelPlacement> peers,
        WorkspaceMetrics metrics,
        bool horizontal)
    {
        var best = double.MaxValue;
        var winner = value;

        Consider(metrics.Padding, value, ref best, ref winner);
        Consider(surfaceExtent - metrics.Padding - extent, value, ref best, ref winner);

        foreach (var peer in peers)
        {
            var near = horizontal ? peer.Left : peer.Top;
            var far = horizontal ? peer.Right : peer.Bottom;

            Consider(near, value, ref best, ref winner);                        // align leading edges
            Consider(far - extent, value, ref best, ref winner);                // align trailing edges
            Consider(far + metrics.Gap, value, ref best, ref winner);           // sit after the peer
            Consider(near - extent - metrics.Gap, value, ref best, ref winner); // sit before the peer
        }

        return best is double.MaxValue ? SnapToGrid(value) : winner;
    }

    private static double SnapResizeAxis(
        double value,
        double surfaceExtent,
        IReadOnlyList<PanelPlacement> peers,
        WorkspaceMetrics metrics,
        bool horizontal,
        bool leading)
    {
        var best = double.MaxValue;
        var winner = value;

        Consider(leading ? metrics.Padding : surfaceExtent - metrics.Padding, value, ref best, ref winner);

        foreach (var peer in peers)
        {
            var near = horizontal ? peer.Left : peer.Top;
            var far = horizontal ? peer.Right : peer.Bottom;

            if (leading)
            {
                Consider(near, value, ref best, ref winner);
                Consider(far + metrics.Gap, value, ref best, ref winner);
            }
            else
            {
                Consider(far, value, ref best, ref winner);
                Consider(near - metrics.Gap, value, ref best, ref winner);
            }
        }

        return best is double.MaxValue ? SnapToGrid(value) : winner;
    }

    private static void Consider(double candidate, double value, ref double best, ref double winner)
    {
        var distance = Math.Abs(candidate - value);
        if (distance <= SnapRadius && distance < best)
        {
            best = distance;
            winner = candidate;
        }
    }
}
