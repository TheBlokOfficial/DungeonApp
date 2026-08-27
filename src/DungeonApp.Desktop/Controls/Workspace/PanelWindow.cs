using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls.Workspace;

/// <summary>
/// One floating panel on the campaign desk: chrome, resize grips, and the whole pointer gesture.
/// Not an OS window — it lives on the surface's <see cref="Canvas"/>.
/// <para>
/// Position and size deliberately reuse the framework's own properties (<see cref="Canvas.LeftProperty"/>,
/// <see cref="Layoutable.WidthProperty"/>, <see cref="Visual.ZIndexProperty"/>) rather than
/// introducing parallel ones, so there is exactly one source of truth per value.
/// </para>
/// </summary>
[PseudoClasses(":active", ":minimized", ":maximized", ":dragging", ":resizing", ":snapping")]
[TemplatePart("PART_TitleBar", typeof(Control))]
[TemplatePart("PART_MinimizeButton", typeof(Button))]
[TemplatePart("PART_MaximizeButton", typeof(Button))]
[TemplatePart("PART_ResizeW", typeof(Control))]
[TemplatePart("PART_ResizeE", typeof(Control))]
[TemplatePart("PART_ResizeN", typeof(Control))]
[TemplatePart("PART_ResizeS", typeof(Control))]
[TemplatePart("PART_ResizeNW", typeof(Control))]
[TemplatePart("PART_ResizeNE", typeof(Control))]
[TemplatePart("PART_ResizeSW", typeof(Control))]
[TemplatePart("PART_ResizeSE", typeof(Control))]
public class PanelWindow : ContentControl
{
    /// <summary>
    /// Raised once when a drag or resize finishes. The feature listens for it to copy the effective
    /// placement into the desired one and to mark the layout dirty. This is the one interaction that
    /// commands model badly, which is why it is an event and everything else is a command.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> GestureCompletedEvent =
        RoutedEvent.Register<PanelWindow, RoutedEventArgs>(nameof(GestureCompleted), RoutingStrategies.Bubble);

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PanelWindow, string?>(nameof(Title));

    public static readonly StyledProperty<string?> IconResourceKeyProperty =
        AvaloniaProperty.Register<PanelWindow, string?>(nameof(IconResourceKey));

    public static readonly StyledProperty<PanelDisplayState> PanelStateProperty =
        AvaloniaProperty.Register<PanelWindow, PanelDisplayState>(nameof(PanelState));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<PanelWindow, bool>(nameof(IsActive));

    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<PanelWindow, bool>(nameof(CanResize), defaultValue: true);

    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<PanelWindow, bool>(nameof(CanMinimize), defaultValue: true);

    public static readonly StyledProperty<ICommand?> ActivateCommandProperty =
        AvaloniaProperty.Register<PanelWindow, ICommand?>(nameof(ActivateCommand));

    public static readonly StyledProperty<ICommand?> MinimizeCommandProperty =
        AvaloniaProperty.Register<PanelWindow, ICommand?>(nameof(MinimizeCommand));

    public static readonly StyledProperty<ICommand?> ToggleMaximizeCommandProperty =
        AvaloniaProperty.Register<PanelWindow, ICommand?>(nameof(ToggleMaximizeCommand));

    private static readonly IReadOnlyDictionary<string, PanelEdge> ResizeParts =
        new Dictionary<string, PanelEdge>(StringComparer.Ordinal)
        {
            ["PART_ResizeW"] = PanelEdge.West,
            ["PART_ResizeE"] = PanelEdge.East,
            ["PART_ResizeN"] = PanelEdge.North,
            ["PART_ResizeS"] = PanelEdge.South,
            ["PART_ResizeNW"] = PanelEdge.NorthWest,
            ["PART_ResizeNE"] = PanelEdge.NorthEast,
            ["PART_ResizeSW"] = PanelEdge.SouthWest,
            ["PART_ResizeSE"] = PanelEdge.SouthEast
        };

    private readonly List<Control> _gestureParts = [];
    private readonly Stopwatch _snapAnimationClock = new();

    private Canvas? _canvas;
    private DispatcherTimer? _snapAnimationTimer;
    private PanelPlacement _snapAnimationFrom;
    private PanelPlacement _snapAnimationTarget;
    private Point _grabOrigin;
    private PanelPlacement _pressPlacement;
    private PanelEdge _edge;
    private WorkspaceMetrics _metrics = WorkspaceMetrics.Fallback;
    private Cursor? _cursorBeforeGesture;
    private bool _gestureActive;
    private bool _gestureOverridesCursor;

    public PanelWindow()
    {
        // Tunnelling, and deliberately never sets Handled: the press must continue to whatever
        // Button or TextBox was actually clicked. A bubbling handler would miss those entirely,
        // because they mark the event handled before it reaches us.
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    public event EventHandler<RoutedEventArgs>? GestureCompleted
    {
        add => AddHandler(GestureCompletedEvent, value);
        remove => RemoveHandler(GestureCompletedEvent, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? IconResourceKey
    {
        get => GetValue(IconResourceKeyProperty);
        set => SetValue(IconResourceKeyProperty, value);
    }

    public PanelDisplayState PanelState
    {
        get => GetValue(PanelStateProperty);
        set => SetValue(PanelStateProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    public ICommand? ActivateCommand
    {
        get => GetValue(ActivateCommandProperty);
        set => SetValue(ActivateCommandProperty, value);
    }

    public ICommand? MinimizeCommand
    {
        get => GetValue(MinimizeCommandProperty);
        set => SetValue(MinimizeCommandProperty, value);
    }

    public ICommand? ToggleMaximizeCommand
    {
        get => GetValue(ToggleMaximizeCommandProperty);
        set => SetValue(ToggleMaximizeCommandProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // OnApplyTemplate can run more than once (theme variant change, Template re-set). Without
        // detaching first, every handler would be attached twice and each gesture would be applied
        // twice per frame.
        DetachGestureParts();

        AttachGesturePart(e.NameScope.Find<Control>("PART_TitleBar"));
        foreach (var partName in ResizeParts.Keys)
        {
            AttachGesturePart(e.NameScope.Find<Control>(partName));
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsActiveProperty)
        {
            PseudoClasses.Set(":active", IsActive);
        }
        else if (change.Property == PanelStateProperty)
        {
            CompleteSnapAnimation();
            PseudoClasses.Set(":minimized", PanelState == PanelDisplayState.Minimized);
            PseudoClasses.Set(":maximized", PanelState == PanelDisplayState.Maximized);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_gestureActive || _canvas is null)
        {
            return;
        }

        // Sampled against the Canvas, which does not move. Sampling against `this` would be a
        // positive feedback loop, because `this` is exactly what the gesture is moving.
        // Always press-rect + total delta, never current-rect + per-frame delta: the latter folds
        // each frame's clamp correction back into the accumulator and the panel drifts.
        var position = e.GetPosition(_canvas);
        var deltaX = position.X - _grabOrigin.X;
        var deltaY = position.Y - _grabOrigin.Y;

        var surfaceWidth = _canvas.Bounds.Width;
        var surfaceHeight = _canvas.Bounds.Height;
        var placement = _edge == PanelEdge.None
            ? PanelGeometry.ClampMove(
                new PanelPlacement(
                    _pressPlacement.X + deltaX,
                    _pressPlacement.Y + deltaY,
                    _pressPlacement.Width,
                    _pressPlacement.Height),
                surfaceWidth,
                surfaceHeight,
                _metrics)
            : PanelGeometry.ConstrainResize(
                PanelGeometry.ResizeRaw(_pressPlacement, _edge, deltaX, deltaY),
                _edge,
                surfaceWidth,
                surfaceHeight,
                CurrentConstraints(),
                _metrics);

        Apply(placement);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndGesture(e.Pointer);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        // Mandatory. Alt-Tab, a modal dialog or a cancelled touch silently drop the capture; without
        // this the panel stays stuck in :dragging and the next click resumes the abandoned gesture.
        EndGesture(null);
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind is not
            (PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.RightButtonPressed))
        {
            return;
        }

        // Minimizing removes the panel immediately, so bringing it to the front on pointer-down
        // only produces a one-frame active-border flash before the Button executes its command.
        // Do not mark the event handled: the press still has to reach the Button itself.
        if (IsWithinTemplatePart(e.Source as Visual, "PART_MinimizeButton"))
        {
            return;
        }

        if (ActivateCommand?.CanExecute(null) == true)
        {
            ActivateCommand.Execute(null);
        }
    }

    private void AttachGesturePart(Control? part)
    {
        if (part is null)
        {
            return;
        }

        part.PointerPressed += OnGesturePartPointerPressed;
        _gestureParts.Add(part);
    }

    private void DetachGestureParts()
    {
        foreach (var part in _gestureParts)
        {
            part.PointerPressed -= OnGesturePartPointerPressed;
        }

        _gestureParts.Clear();
    }

    private void OnGesturePartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control part || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var isTitleBar = part.Name == "PART_TitleBar";

        if (isTitleBar && ContainsButtonBetween(e.Source as Visual, part))
        {
            // Header buttons must not also start a drag.
            return;
        }

        if (isTitleBar && e.ClickCount == 2)
        {
            if (ToggleMaximizeCommand?.CanExecute(null) == true)
            {
                ToggleMaximizeCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        var edge = isTitleBar ? PanelEdge.None : ResizeParts.GetValueOrDefault(part.Name ?? string.Empty);
        if (!isTitleBar && (edge == PanelEdge.None || !CanResize))
        {
            return;
        }

        if (PanelState == PanelDisplayState.Maximized || PanelState == PanelDisplayState.Minimized)
        {
            return;
        }

        // A second gesture during the short settle animation starts exactly where the panel is
        // currently painted. The interrupted position is committed before the new gesture owns it.
        CancelSnapAnimation(commitCurrent: true);

        _canvas = this.FindAncestorOfType<Canvas>();
        if (_canvas is null)
        {
            return;
        }

        _edge = edge;
        _metrics = WorkspaceMetricsResolver.Resolve();
        _grabOrigin = e.GetPosition(_canvas);
        _pressPlacement = CurrentPlacement();
        _gestureActive = true;

        if (_edge != PanelEdge.None)
        {
            _cursorBeforeGesture = Cursor;
            _gestureOverridesCursor = true;
            Cursor = part.Cursor;
        }

        // Captured on the panel itself, so every subsequent pointer event routes straight here and
        // no per-part subscription bookkeeping is needed for the rest of the gesture.
        e.Pointer.Capture(this);
        PseudoClasses.Set(":dragging", _edge == PanelEdge.None);
        PseudoClasses.Set(":resizing", _edge != PanelEdge.None);
        e.Handled = true;
    }

    private void EndGesture(IPointer? pointer)
    {
        if (!_gestureActive)
        {
            return;
        }

        _gestureActive = false;
        pointer?.Capture(null);
        RestoreGestureCursor();
        PseudoClasses.Set(":dragging", false);
        PseudoClasses.Set(":resizing", false);

        if (_canvas is { } canvas && _edge == PanelEdge.None)
        {
            var target = PanelGeometry.SnapMove(
                CurrentPlacement(),
                canvas.Bounds.Width,
                canvas.Bounds.Height,
                CollectPeers(),
                _metrics);

            _canvas = null;
            StartSnapAnimation(CurrentPlacement(), target);
            return;
        }

        if (_canvas is { } resizeCanvas && _edge != PanelEdge.None)
        {
            var target = PanelGeometry.SnapResize(
                CurrentPlacement(),
                _edge,
                resizeCanvas.Bounds.Width,
                resizeCanvas.Bounds.Height,
                CollectPeers(),
                CurrentConstraints(),
                _metrics);

            _canvas = null;
            StartSnapAnimation(CurrentPlacement(), target);
            return;
        }

        _canvas = null;
        RaiseGestureCompleted();
    }

    private void StartSnapAnimation(PanelPlacement from, PanelPlacement target)
    {
        if (from == target || WorkspaceGridSettings.SnapAnimationDurationMilliseconds <= 0)
        {
            Apply(target);
            RaiseGestureCompleted();
            return;
        }

        _snapAnimationFrom = from;
        _snapAnimationTarget = target;
        _snapAnimationClock.Restart();
        PseudoClasses.Set(":snapping", true);

        _snapAnimationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000d / 60d)
        };
        _snapAnimationTimer.Tick += OnSnapAnimationTick;
        _snapAnimationTimer.Start();
    }

    private void OnSnapAnimationTick(object? sender, EventArgs e)
    {
        var progress = Math.Min(
            1,
            _snapAnimationClock.Elapsed.TotalMilliseconds /
            WorkspaceGridSettings.SnapAnimationDurationMilliseconds);
        var eased = 1 - Math.Pow(1 - progress, 3);

        Apply(new PanelPlacement(
            Lerp(_snapAnimationFrom.X, _snapAnimationTarget.X, eased),
            Lerp(_snapAnimationFrom.Y, _snapAnimationTarget.Y, eased),
            Lerp(_snapAnimationFrom.Width, _snapAnimationTarget.Width, eased),
            Lerp(_snapAnimationFrom.Height, _snapAnimationTarget.Height, eased)));

        if (progress >= 1)
        {
            CompleteSnapAnimation();
        }
    }

    private void CompleteSnapAnimation()
    {
        if (_snapAnimationTimer is null)
        {
            return;
        }

        StopSnapAnimationTimer();
        Apply(_snapAnimationTarget);
        RaiseGestureCompleted();
    }

    private void CancelSnapAnimation(bool commitCurrent)
    {
        if (_snapAnimationTimer is null)
        {
            return;
        }

        StopSnapAnimationTimer();
        if (commitCurrent)
        {
            RaiseGestureCompleted();
        }
    }

    private void StopSnapAnimationTimer()
    {
        _snapAnimationTimer?.Stop();
        if (_snapAnimationTimer is not null)
        {
            _snapAnimationTimer.Tick -= OnSnapAnimationTick;
        }

        _snapAnimationTimer = null;
        _snapAnimationClock.Reset();
        PseudoClasses.Set(":snapping", false);
    }

    private void RaiseGestureCompleted() =>
        RaiseEvent(new RoutedEventArgs(GestureCompletedEvent, this));

    private void RestoreGestureCursor()
    {
        if (!_gestureOverridesCursor)
        {
            return;
        }

        Cursor = _cursorBeforeGesture;
        _cursorBeforeGesture = null;
        _gestureOverridesCursor = false;
    }

    private static double Lerp(double start, double end, double amount) =>
        start + ((end - start) * amount);

    private PanelPlacement CurrentPlacement() =>
        new(Canvas.GetLeft(this), Canvas.GetTop(this), Bounds.Width, Bounds.Height);

    private PanelConstraints CurrentConstraints() =>
        new(
            Math.Max(MinWidth, _metrics.MinPanelWidth),
            Math.Max(MinHeight, _metrics.MinPanelHeight),
            MaxWidth,
            MaxHeight);

    private void Apply(PanelPlacement placement)
    {
        // Local values, which is exactly why the surface creates its bindings in code: a TwoWay
        // binding at LocalValue priority carries these straight through to the view model, whereas
        // a theme setter would be outranked here and the view model would stop receiving updates.
        Canvas.SetLeft(this, placement.X);
        Canvas.SetTop(this, placement.Y);
        Width = placement.Width;
        Height = placement.Height;
    }

    private List<PanelPlacement> CollectPeers()
    {
        var peers = new List<PanelPlacement>();
        if (_canvas is null)
        {
            return peers;
        }

        foreach (var child in _canvas.Children)
        {
            if (child is not PanelWindow peer
                || ReferenceEquals(peer, this)
                || peer.PanelState == PanelDisplayState.Minimized
                || !peer.IsVisible)
            {
                continue;
            }

            peers.Add(peer.CurrentPlacement());
        }

        return peers;
    }

    private static bool ContainsButtonBetween(Visual? source, Control boundary)
    {
        for (var current = source; current is not null && !ReferenceEquals(current, boundary); current = current.GetVisualParent())
        {
            if (current is Button)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWithinTemplatePart(Visual? source, string partName)
    {
        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Control { Name: var name } && name == partName)
            {
                return true;
            }

            if (ReferenceEquals(current, this))
            {
                break;
            }
        }

        return false;
    }
}
