using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Workspace.Controls;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>
/// The world catalog on the desk. The code-behind holds what is a view concern: which row a press
/// hit and with which modifiers, the drag of the root row (the catalog), the drag of other rows (the
/// selection into a folder) and of the right edge, keys, the context menu and the windows an action
/// needs (palette, confirmation); every decision goes to <see cref="WorldCatalogViewModel"/>.
/// </summary>
public partial class WorldCatalogView : UserControl
{
    /// <summary>Distance the pointer must travel before a press on a row becomes a drag, not a click.</summary>
    private const double DragThreshold = 8;

    /// <summary>How long a dragged selection rests on a collapsed folder before the folder opens.</summary>
    private static readonly TimeSpan HoverExpandDelay = TimeSpan.FromMilliseconds(600);

    /// <summary>Height of the band at the tree's top and bottom edge where a drag scrolls the tree.</summary>
    private const double AutoScrollBand = 32;

    /// <summary>Scroll per tick at the very edge; it falls off towards the inner side of the band.</summary>
    private const double AutoScrollMaxStep = 14;

    /// <summary>Where the copy of the dragged row sits relative to the pointer: below and to the right, off the target.</summary>
    private static readonly Point GhostOffset = new(14, 10);

    private enum Gesture
    {
        None,
        Move,
        Resize,
        DragRows,
    }

    private readonly ContextMenu _menu = new();

    private Gesture _gesture;
    private bool _gestureStarted;
    private Point _pressPoint;
    private double _startX;
    private double _startY;
    private double _startWidth;
    private WorldRowViewModel? _dragRow;
    private WorldRowViewModel? _deferredClick;
    private WorldRowViewModel? _dropTarget;
    private WorldRowViewModel? _hoverRow;
    private IPointer? _dragPointer;
    private Point _dragPoint;
    private DispatcherTimer? _expandTimer;
    private DispatcherTimer? _scrollTimer;
    private TopLevel? _topLevel;
    private WorldRowViewModel? _renameRow;

    // The settle after a move or a resize: the catalog glides to the grid like a window does.
    private readonly Stopwatch _snapClock = new();
    private DispatcherTimer? _snapTimer;
    private double _snapFromX;
    private double _snapFromY;
    private double _snapFromWidth;
    private double _snapToX;
    private double _snapToY;
    private double _snapToWidth;
    private bool _snapIsResize;

    public WorldCatalogView()
    {
        InitializeComponent();

        ContextMenu = _menu;
        AddHandler(PointerPressedEvent, OnPointerPressed);
        AddHandler(PointerMovedEvent, OnPointerMoved);
        AddHandler(PointerReleasedEvent, OnPointerReleased);
        AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
        AddHandler(Button.ClickEvent, OnButtonClick);
        AddHandler(KeyDownEvent, OnRenameKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown);
        AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        AddHandler(GotFocusEvent, OnGotFocus);
        ResizeGrip.PointerPressed += OnResizePressed;
    }

    private WorldCatalogViewModel? ViewModel => DataContext as WorldCatalogViewModel;

    /// <summary>The context menu as the last right click built it.</summary>
    internal ContextMenu Menu => _menu;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (ViewModel is { } viewModel)
        {
            viewModel.RenameStarted += OnRenameStarted;
            viewModel.RevealRequested += ScrollToSelection;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Tunnelling at the window, so a press anywhere is seen before the control it lands on.
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        CompleteSnap();
        CancelRowDrag();
        StopWaitingForRenameField();
        _topLevel?.RemoveHandler(PointerPressedEvent, OnWindowPointerPressed);
        _topLevel = null;

        if (ViewModel is { } viewModel)
        {
            viewModel.RenameStarted -= OnRenameStarted;
            viewModel.RevealRequested -= ScrollToSelection;
        }
    }

    /// <summary>Opens the add palette for <paramref name="target"/>; the palette finds its overlay through this view's window.</summary>
    public void ShowAddPalette(WorldAddTarget target)
    {
        if (ViewModel is { } viewModel)
        {
            _ = Palette.ShowAsync(this, viewModel.CreateAddOptions(target));
        }
    }

    private Visual? Surface => this.FindAncestorOfType<WorkspaceSurface>() ?? this.GetVisualParent();

    // ---- Pointer ----

    // A press outside the open rename field saves the typed name, as in a file explorer (Escape drops it);
    // the press goes on to whatever it landed on. The selection stays: a press off the catalog (the desk,
    // a window) is not a choice made in the tree.
    // Async void on purpose: a failed save reaches the UI thread's error handler instead of vanishing.
    private async void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is { } viewModel && e.Source is Visual source && !IsInsideField(source))
        {
            await viewModel.CommitRenamesAsync();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        CompleteSnap();

        if (ViewModel is not { } viewModel || Surface is not { } surface || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is not Visual source || IsInsideField(source))
        {
            return;
        }

        if (RowOf(source) is not { } row)
        {
            // Empty space of the tree (the scroll bar is not empty space).
            if (!source.GetSelfAndVisualAncestors().OfType<ScrollBar>().Any())
            {
                Focus();
                viewModel.ClearSelection();
            }

            return;
        }

        Focus();

        if (e.ClickCount >= 2)
        {
            viewModel.Activate(row);
            e.Handled = true;
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var plain = !ctrl && !shift;

        // Pressing a row of a multiple selection keeps the selection: it may be about to be dragged.
        if (!row.IsRoot && plain && row.IsSelected && viewModel.SelectedRows.Count > 1)
        {
            _deferredClick = row;
        }
        else
        {
            _deferredClick = null;
            viewModel.Click(row, ctrl, shift);
        }

        if (row.IsRoot)
        {
            // The root row is the catalog's handle: pressing it can start moving the whole catalog.
            _gesture = Gesture.Move;
            _gestureStarted = false;
            _pressPoint = e.GetPosition(surface);
            _startX = viewModel.X;
            _startY = viewModel.Y;
            e.Pointer.Capture(this);
        }
        else if (plain)
        {
            _gesture = Gesture.DragRows;
            _gestureStarted = false;
            _dragRow = row;
            _dragPointer = e.Pointer;
            _pressPoint = e.GetPosition(this);
            e.Pointer.Capture(this);
        }

        e.Handled = true;
    }

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
        CompleteSnap();

        if (ViewModel is not { } viewModel || Surface is not { } surface || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        viewModel.BeginMove();
        _gesture = Gesture.Resize;
        _gestureStarted = true;
        _pressPoint = e.GetPosition(surface);
        _startWidth = viewModel.Width;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_gesture == Gesture.None || ViewModel is not { } viewModel || Surface is not { } surface)
        {
            return;
        }

        if (_gesture == Gesture.DragRows)
        {
            var point = e.GetPosition(this);
            var travelled = point - _pressPoint;

            if (!_gestureStarted && travelled.X * travelled.X + travelled.Y * travelled.Y >= DragThreshold * DragThreshold)
            {
                _gestureStarted = true;
                BeginRowDrag(viewModel);
            }

            if (_gestureStarted)
            {
                UpdateRowDrag(viewModel, point);
            }

            return;
        }

        var delta = e.GetPosition(surface) - _pressPoint;

        if (_gesture == Gesture.Resize)
        {
            viewModel.ResizeTo(_startWidth + delta.X, commit: false);
            return;
        }

        if (!_gestureStarted)
        {
            if (delta.X * delta.X + delta.Y * delta.Y < DragThreshold * DragThreshold)
            {
                return;
            }

            _gestureStarted = true;
            viewModel.BeginMove();
        }

        viewModel.MoveTo(_startX + delta.X, _startY + delta.Y, commit: false);
    }

    // Async void on purpose: a failed save reaches the UI thread's error handler instead of vanishing.
    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_gesture == Gesture.None || ViewModel is not { } viewModel || Surface is not { } surface)
        {
            return;
        }

        // Everything the gesture knows is read before the capture is released: releasing it raises
        // PointerCaptureLost at once, and that ends (cancels) a gesture still in progress.
        var delta = e.GetPosition(surface) - _pressPoint;
        var gesture = _gesture;
        var started = _gestureStarted;
        var dragged = _dragRow;
        var deferred = _deferredClick;
        var target = gesture == Gesture.DragRows && started ? DropRowAt(viewModel, e.GetPosition(this)) : null;
        _gesture = Gesture.None;
        EndRowDrag();
        e.Pointer.Capture(null);

        if (gesture == Gesture.Resize)
        {
            StartSnap(viewModel, viewModel.X, viewModel.Y, viewModel.Width, viewModel.X, viewModel.Y, viewModel.SnapTargetOfWidth(_startWidth + delta.X), isResize: true);
        }
        else if (gesture == Gesture.DragRows)
        {
            if (started && dragged is not null && target is not null)
            {
                await viewModel.DropAsync(dragged, target);
                ScrollToSelection();
            }
            else if (!started && deferred is not null)
            {
                viewModel.Click(deferred, ctrl: false, shift: false);
            }
        }
        else if (started)
        {
            var place = viewModel.SnapTargetOfMove(_startX + delta.X, _startY + delta.Y);
            StartSnap(viewModel, viewModel.X, viewModel.Y, viewModel.Width, place.X, place.Y, viewModel.Width, isResize: false);
        }
    }

    private void StartSnap(
        WorldCatalogViewModel viewModel, double fromX, double fromY, double fromWidth, double toX, double toY, double toWidth, bool isResize)
    {
        _snapFromX = fromX;
        _snapFromY = fromY;
        _snapFromWidth = fromWidth;
        _snapToX = toX;
        _snapToY = toY;
        _snapToWidth = toWidth;
        _snapIsResize = isResize;

        var unchanged = fromX == toX && fromY == toY && fromWidth == toWidth;

        if (unchanged || WorkspaceGridSettings.SnapAnimationDurationMilliseconds <= 0)
        {
            CommitSnap(viewModel);
            return;
        }

        _snapClock.Restart();
        _snapTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 60d) };
        _snapTimer.Tick += OnSnapTick;
        _snapTimer.Start();
    }

    private void OnSnapTick(object? sender, EventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            StopSnapTimer();
            return;
        }

        var progress = Math.Min(1, _snapClock.Elapsed.TotalMilliseconds / WorkspaceGridSettings.SnapAnimationDurationMilliseconds);
        var eased = 1 - Math.Pow(1 - progress, 3);

        if (progress >= 1)
        {
            CompleteSnap();
            return;
        }

        if (_snapIsResize)
        {
            viewModel.ResizeTo(_snapFromWidth + ((_snapToWidth - _snapFromWidth) * eased), commit: false);
        }
        else
        {
            viewModel.MoveTo(
                _snapFromX + ((_snapToX - _snapFromX) * eased), _snapFromY + ((_snapToY - _snapFromY) * eased), commit: false);
        }
    }

    /// <summary>Ends a running settle at once, at its target, and saves the place.</summary>
    private void CompleteSnap()
    {
        if (_snapTimer is null)
        {
            return;
        }

        StopSnapTimer();

        if (ViewModel is { } viewModel)
        {
            CommitSnap(viewModel);
        }
    }

    private void CommitSnap(WorldCatalogViewModel viewModel)
    {
        if (_snapIsResize)
        {
            viewModel.ResizeTo(_snapToWidth, commit: true);
        }
        else
        {
            viewModel.MoveTo(_snapToX, _snapToY, commit: true);
        }
    }

    private void StopSnapTimer()
    {
        if (_snapTimer is null)
        {
            return;
        }

        _snapTimer.Stop();
        _snapTimer.Tick -= OnSnapTick;
        _snapTimer = null;
        _snapClock.Reset();
    }

    // A capture taken away mid-gesture (another window, the system) cancels it; nothing moves.
    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _gesture = Gesture.None;
        EndRowDrag();
    }

    // ---- Dragging rows ----

    private void BeginRowDrag(WorldCatalogViewModel viewModel)
    {
        if (_dragRow is not { } dragged)
        {
            return;
        }

        viewModel.MarkDragSources(dragged);

        var count = viewModel.DragCount(dragged);
        DragGhost.DataContext = dragged;
        DragGhostCount.Text = count > 1 ? $"+{count - 1}" : string.Empty;
        DragGhostCount.IsVisible = count > 1;
        DragGhost.IsVisible = true;

        _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 60d) };
        _scrollTimer.Tick += OnAutoScrollTick;
        _scrollTimer.Start();
    }

    private void UpdateRowDrag(WorldCatalogViewModel viewModel, Point point)
    {
        _dragPoint = point;
        DragGhost.Margin = new Thickness(point.X + GhostOffset.X, point.Y + GhostOffset.Y, 0, 0);

        var over = RowAt(point);
        var target = DropRowAt(viewModel, point);

        if (!ReferenceEquals(target, _dropTarget))
        {
            if (_dropTarget is not null)
            {
                _dropTarget.IsDropTarget = false;
            }

            _dropTarget = target;

            if (target is not null)
            {
                target.IsDropTarget = true;
            }
        }

        if (!ReferenceEquals(over, _hoverRow))
        {
            _hoverRow = over;
            StopExpandTimer();

            if (over is { IsFolderLike: true, IsExpanded: false } && _dragRow is { } dragged && viewModel.CanDrop(dragged, over))
            {
                _expandTimer = new DispatcherTimer { Interval = HoverExpandDelay };
                _expandTimer.Tick += OnExpandTick;
                _expandTimer.Start();
            }
        }
    }

    /// <summary>The row under <paramref name="point"/> (this view's coordinates) inside the tree's viewport, or null.</summary>
    private WorldRowViewModel? RowAt(Point point)
    {
        if (this.TranslatePoint(point, Tree) is not { } inTree || !new Rect(Tree.Bounds.Size).Contains(inTree))
        {
            return null;
        }

        return this.InputHitTest(point) is Visual hit ? RowOf(hit) : null;
    }

    /// <summary>The folder row a drop at <paramref name="point"/> would land in, or null when the place refuses it.</summary>
    private WorldRowViewModel? DropRowAt(WorldCatalogViewModel viewModel, Point point) =>
        RowAt(point) is { } over && _dragRow is { } dragged && viewModel.CanDrop(dragged, over) ? viewModel.DropRowOf(over) : null;

    // Resting on a collapsed folder opens it, so a drag can go deeper without letting go.
    private void OnExpandTick(object? sender, EventArgs e)
    {
        StopExpandTimer();

        if (_gesture == Gesture.DragRows && ViewModel is { } viewModel && _dragRow is { } dragged
            && _hoverRow is { IsFolderLike: true, IsExpanded: false } folder)
        {
            viewModel.SetExpanded(folder, true);
            viewModel.MarkDragSources(dragged);
            UpdateRowDrag(viewModel, _dragPoint);
        }
    }

    // Near the tree's top or bottom edge the tree scrolls, faster the closer the pointer is to the edge.
    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (_gesture != Gesture.DragRows || ViewModel is not { } viewModel || this.TranslatePoint(_dragPoint, Tree) is not { } inTree)
        {
            return;
        }

        var height = Tree.Bounds.Height;

        if (inTree.X < 0 || inTree.X > Tree.Bounds.Width || height <= 2 * AutoScrollBand)
        {
            return;
        }

        var step = 0.0;

        if (inTree.Y < AutoScrollBand)
        {
            step = -AutoScrollMaxStep * Math.Min(1, (AutoScrollBand - inTree.Y) / AutoScrollBand);
        }
        else if (inTree.Y > height - AutoScrollBand)
        {
            step = AutoScrollMaxStep * Math.Min(1, (inTree.Y - (height - AutoScrollBand)) / AutoScrollBand);
        }

        var maxOffset = Math.Max(0, Tree.Extent.Height - Tree.Viewport.Height);
        var offset = Math.Clamp(Tree.Offset.Y + step, 0, maxOffset);

        if (offset == Tree.Offset.Y)
        {
            return;
        }

        Tree.Offset = new Vector(Tree.Offset.X, offset);
        Tree.UpdateLayout();
        UpdateRowDrag(viewModel, _dragPoint);
    }

    /// <summary>Esc during a drag: nothing moves, the faded rows come back.</summary>
    private void CancelRowDrag()
    {
        if (_gesture != Gesture.DragRows)
        {
            return;
        }

        _gesture = Gesture.None;
        var pointer = _dragPointer;
        EndRowDrag();
        pointer?.Capture(null);
    }

    /// <summary>Clears every trace of a row drag: the target frame, the faded rows, the copy, the timers and the cursor.</summary>
    private void EndRowDrag()
    {
        if (_dropTarget is not null)
        {
            _dropTarget.IsDropTarget = false;
        }

        if (_dragRow is not null)
        {
            ViewModel?.ClearDragSources();
        }

        StopExpandTimer();

        if (_scrollTimer is not null)
        {
            _scrollTimer.Stop();
            _scrollTimer.Tick -= OnAutoScrollTick;
            _scrollTimer = null;
        }

        DragGhost.IsVisible = false;
        DragGhost.DataContext = null;
        _dropTarget = null;
        _hoverRow = null;
        _dragRow = null;
        _dragPointer = null;
        _deferredClick = null;
    }

    private void StopExpandTimer()
    {
        if (_expandTimer is null)
        {
            return;
        }

        _expandTimer.Stop();
        _expandTimer.Tick -= OnExpandTick;
        _expandTimer = null;
    }

    /// <summary>Scrolls the tree so the selected rows (the ones just moved) are in sight, the first of them at least.</summary>
    private void ScrollToSelection()
    {
        UpdateLayout();

        var selected = this.GetVisualDescendants().OfType<Border>()
            .Where(border => border.Classes.Contains("world-row") && border.DataContext is WorldRowViewModel { IsSelected: true })
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        var first = selected.MinBy(border => border.TranslatePoint(default, Tree)?.Y ?? 0)!;
        var last = selected.MaxBy(border => border.TranslatePoint(default, Tree)?.Y ?? 0)!;
        var lastBottom = last.TranslatePoint(new Point(0, last.Bounds.Height), first)?.Y ?? first.Bounds.Height;

        // Never taller than the viewport, so the first moved row is the one that surely shows.
        var height = Math.Min(Math.Max(lastBottom, first.Bounds.Height), Tree.Viewport.Height);
        first.BringIntoView(new Rect(0, 0, first.Bounds.Width, height));
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Source is not Button { Tag: string tag, DataContext: WorldRowViewModel row })
        {
            return;
        }

        Focus();

        if (tag == "toggle")
        {
            viewModel.ToggleExpanded(row);
        }

        e.Handled = true;
    }

    // ---- Context menu ----

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Source is not Visual source)
        {
            return;
        }

        if (IsInsideField(source))
        {
            return;
        }

        if (RowOf(source) is not { } row)
        {
            e.Handled = true;
            return;
        }

        viewModel.SelectForMenu(row);
        RebuildMenu();
        e.Handled = _menu.Items.Count == 0;
    }

    /// <summary>Fills <see cref="Menu"/> from the view model's entries for the current selection.</summary>
    internal void RebuildMenu()
    {
        _menu.Items.Clear();

        if (ViewModel is not { } viewModel)
        {
            return;
        }

        foreach (var entry in viewModel.CreateMenu())
        {
            if (entry.IsDanger)
            {
                _menu.Items.Add(new Separator());
            }

            _menu.Items.Add(CreateItem(entry));
        }
    }

    private MenuItem CreateItem(WorldMenuEntry entry)
    {
        var item = new MenuItem { Header = entry.Header, IsEnabled = entry.IsEnabled };

        if (entry.IsDanger)
        {
            item.Classes.Add("danger");
        }

        if (entry.IsEnabled)
        {
            item.InputGesture = entry.Gesture;
        }
        else if (entry.DisabledReason is { } reason)
        {
            // The reason takes the shortcut's place; an item never shows both.
            MenuItemTrailing.SetText(item, reason);
        }

        // Async void on purpose: a failed save reaches the UI thread's error handler instead of vanishing.
        item.Click += async (_, _) => await ExecuteAsync(entry.Command);
        return item;
    }

    /// <summary>Runs a menu command on the current selection.</summary>
    internal async Task ExecuteAsync(WorldCommand command)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        switch (command)
        {
            case WorldCommand.Add:
                ShowAddPalette(viewModel.ResolveAddTarget());
                break;
            case WorldCommand.NewFolder:
                await viewModel.CreateFolderInTargetAsync();
                break;
            case WorldCommand.Open when viewModel.PrimaryRow is { } row:
                viewModel.Activate(row);
                break;
            case WorldCommand.Rename:
                RenameSelected();
                break;
            case WorldCommand.MoveTo:
                _ = Palette.ShowAsync(this, viewModel.CreateMoveOptions());
                break;
            case WorldCommand.Delete:
                await DeleteSelectedAsync();
                break;
        }
    }

    private void RenameSelected()
    {
        if (ViewModel is { PrimaryRow: { IsRoot: false } row } viewModel && viewModel.SelectedRows.Count(candidate => !candidate.IsRoot) == 1)
        {
            viewModel.BeginRename(row);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (ViewModel is not { } viewModel || viewModel.DeleteProblem() is not null || viewModel.SelectedRows.All(row => row.IsRoot))
        {
            return;
        }

        var (title, message) = viewModel.DeleteConfirmation();

        if (await ConfirmationDialog.ShowAsync(this, title, message, "Usuń", isDestructive: true))
        {
            await viewModel.DeleteSelectionAsync();
        }
    }

    // ---- Rename in place ----

    // Background priority: a rename started from the context menu must take focus after the closing
    // menu hands focus back to the tree, or the field opens unfocused.
    // The field takes focus after the layout pass that shows it, not at some dispatcher priority: the field
    // is hidden until the rename starts, so its template is applied only by that pass, and on Windows the
    // pass can wait for the compositor while lower-priority jobs run. A text box focused before its
    // template has no caret and takes no typing. A new folder's row may not even exist before the pass.
    private void OnRenameStarted(WorldRowViewModel row)
    {
        _renameRow = row;
        LayoutUpdated -= OnRenameLayoutUpdated;
        LayoutUpdated += OnRenameLayoutUpdated;
    }

    private void OnRenameLayoutUpdated(object? sender, EventArgs e)
    {
        if (_renameRow is not { IsEditing: true } row)
        {
            StopWaitingForRenameField();
            return;
        }

        if (ReadyRenameField(row) is { } box)
        {
            StopWaitingForRenameField();
            box.Focus();
            box.SelectAll();
        }
    }

    private void StopWaitingForRenameField()
    {
        LayoutUpdated -= OnRenameLayoutUpdated;
        _renameRow = null;
    }

    // While a rename is open, focus that comes back to the catalog itself (the closing context menu or the
    // window regaining activation restores it there) goes on to the field, so typing never lands on the tree.
    // Enter and Escape close the rename before they focus the catalog, so they pass.
    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, this) || ViewModel?.Rows.FirstOrDefault(row => row.IsEditing) is not { } row)
        {
            return;
        }

        if (ReadyRenameField(row) is { } box)
        {
            box.Focus();

            // Losing focus dropped the selection; an untouched name is selected again, so typing still replaces it.
            if (row.EditText == row.Name)
            {
                box.SelectAll();
            }
        }
    }

    /// <summary>The rename field of <paramref name="row"/> once it is shown, templated and arranged; otherwise null.</summary>
    private TextBox? ReadyRenameField(WorldRowViewModel row) =>
        this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(box => ReferenceEquals(box.DataContext, row)) is
            { IsEffectivelyVisible: true, IsArrangeValid: true } box && box.GetVisualChildren().Any()
            ? box
            : null;

    // Tunnelling, so Enter and Escape reach the catalog before the field's own handling of them.
    private async void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Source is not TextBox { DataContext: WorldRowViewModel row })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;

            if (await viewModel.CommitRenameAsync(row))
            {
                Focus();
            }
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            viewModel.CancelRename(row);
            Focus();
        }
    }

    // ---- Keys ----

    // Async void on purpose: a failed save reaches the UI thread's error handler instead of vanishing.
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Source is TextBox)
        {
            return;
        }

        if (_gesture == Gesture.DragRows)
        {
            // During a row drag the keys wait; Esc cancels it.
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                CancelRowDrag();
            }

            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.N when ctrl && shift:
                e.Handled = true;
                await viewModel.CreateFolderInTargetAsync();
                break;
            case Key.N when ctrl:
                e.Handled = true;
                ShowAddPalette(viewModel.ResolveAddTarget());
                break;
            case Key.F2:
                e.Handled = true;
                RenameSelected();
                break;
            case Key.Delete:
                e.Handled = true;
                await DeleteSelectedAsync();
                break;
            case Key.Up:
                e.Handled = true;
                viewModel.MoveSelection(-1, shift);
                break;
            case Key.Down:
                e.Handled = true;
                viewModel.MoveSelection(1, shift);
                break;
            case Key.Left when viewModel.PrimaryRow is { IsFolderLike: true } folder:
                e.Handled = true;
                viewModel.SetExpanded(folder, false);
                break;
            case Key.Right when viewModel.PrimaryRow is { IsFolderLike: true } folder:
                e.Handled = true;
                viewModel.SetExpanded(folder, true);
                break;
            case Key.Enter when viewModel.PrimaryRow is { } row:
                e.Handled = true;
                viewModel.Activate(row);
                break;
        }
    }

    private static bool IsInsideField(Visual source) => source.GetSelfAndVisualAncestors().OfType<TextBox>().Any();

    /// <summary>The row whose template contains <paramref name="source"/>, or null (the resize strip, empty space).</summary>
    private static WorldRowViewModel? RowOf(Visual source) =>
        source.GetSelfAndVisualAncestors().OfType<Control>().Select(control => control.DataContext).OfType<WorldRowViewModel>().FirstOrDefault();
}
