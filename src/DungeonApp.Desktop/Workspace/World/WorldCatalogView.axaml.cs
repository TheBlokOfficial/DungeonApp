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
        StopWaitingForRenameField();
        _topLevel?.RemoveHandler(PointerPressedEvent, OnWindowPointerPressed);
        _topLevel = null;

        if (ViewModel is { } viewModel)
        {
            viewModel.RenameStarted -= OnRenameStarted;
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

    // A press outside the open rename field drops the rename; a left press on the desk, off every window
    // and off the catalog, deselects. The press goes on to whatever it landed on.
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is not { } viewModel || e.Source is not Visual source)
        {
            return;
        }

        if (!IsInsideField(source))
        {
            viewModel.CancelRenames();
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            && !source.GetSelfAndVisualAncestors().Any(visual => ReferenceEquals(visual, this) || visual is PanelWindow)
            && source.FindAncestorOfType<WorkspaceSurface>(includeSelf: true) is not null)
        {
            viewModel.ClearSelection();
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
            var travelled = e.GetPosition(this) - _pressPoint;

            if (!_gestureStarted && travelled.X * travelled.X + travelled.Y * travelled.Y >= DragThreshold * DragThreshold)
            {
                _gestureStarted = true;
            }

            if (_gestureStarted)
            {
                HoverDropTarget(viewModel, e.GetPosition(this));
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

        var delta = e.GetPosition(surface) - _pressPoint;
        var gesture = _gesture;
        var started = _gestureStarted;
        _gesture = Gesture.None;
        e.Pointer.Capture(null);

        if (gesture == Gesture.Resize)
        {
            StartSnap(viewModel, viewModel.X, viewModel.Y, viewModel.Width, viewModel.X, viewModel.Y, viewModel.SnapTargetOfWidth(_startWidth + delta.X), isResize: true);
        }
        else if (gesture == Gesture.DragRows)
        {
            var dragged = _dragRow;
            var target = _dropTarget;
            var deferred = _deferredClick;
            ClearDrag();

            if (started && dragged is not null && target is not null)
            {
                await viewModel.DropAsync(dragged, target);
            }
            else if (!started && deferred is not null)
            {
                viewModel.Click(deferred, ctrl: false, shift: false);
            }
        }
        else if (started)
        {
            var target = viewModel.SnapTargetOfMove(_startX + delta.X, _startY + delta.Y);
            StartSnap(viewModel, viewModel.X, viewModel.Y, viewModel.Width, target.X, target.Y, viewModel.Width, isResize: false);
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

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _gesture = Gesture.None;
        ClearDrag();
    }

    private void HoverDropTarget(WorldCatalogViewModel viewModel, Point point)
    {
        var over = this.InputHitTest(point) is Visual hit ? RowOf(hit) : null;
        var target = over is not null && _dragRow is not null && viewModel.CanDrop(_dragRow, over) ? over : null;

        if (ReferenceEquals(target, _dropTarget))
        {
            return;
        }

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

    private void ClearDrag()
    {
        if (_dropTarget is not null)
        {
            _dropTarget.IsDropTarget = false;
        }

        _dropTarget = null;
        _dragRow = null;
        _deferredClick = null;
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
