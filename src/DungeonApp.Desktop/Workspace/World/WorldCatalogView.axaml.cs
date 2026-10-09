using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Workspace.Controls;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>
/// The world catalog on the desk. The code-behind holds what is a view concern: which row a press
/// hit and with which modifiers, the drag of the root row and of the right edge (in desk
/// coordinates), and keys; every decision goes to <see cref="WorldCatalogViewModel"/>.
/// </summary>
public partial class WorldCatalogView : UserControl
{
    /// <summary>Distance the pointer must travel before a press on the root row becomes a drag, not a click.</summary>
    private const double DragThreshold = 4;

    private enum Gesture
    {
        None,
        Move,
        Resize,
    }

    private Gesture _gesture;
    private bool _gestureStarted;
    private Point _pressPoint;
    private double _startX;
    private double _startY;
    private double _startWidth;

    public WorldCatalogView()
    {
        InitializeComponent();

        AddHandler(PointerPressedEvent, OnPointerPressed);
        AddHandler(PointerMovedEvent, OnPointerMoved);
        AddHandler(PointerReleasedEvent, OnPointerReleased);
        AddHandler(PointerCaptureLostEvent, OnPointerCaptureLost);
        AddHandler(Button.ClickEvent, OnButtonClick);
        AddHandler(KeyDownEvent, OnKeyDown);
        ResizeGrip.PointerPressed += OnResizePressed;
    }

    private WorldCatalogViewModel? ViewModel => DataContext as WorldCatalogViewModel;

    /// <summary>Opens the add palette for <paramref name="target"/>; the palette finds its overlay through this view's window.</summary>
    public void ShowAddPalette(WorldAddTarget target)
    {
        if (ViewModel is { } viewModel)
        {
            _ = Palette.ShowAsync(this, viewModel.CreateAddOptions(target));
        }
    }

    private Visual? Surface => this.FindAncestorOfType<WorkspaceSurface>() ?? this.GetVisualParent();

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is not { } viewModel || Surface is not { } surface || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is not Visual source || RowOf(source) is not { } row)
        {
            return;
        }

        Focus();

        if (e.ClickCount >= 2)
        {
            viewModel.Activate(row);
            e.Handled = true;
            return;
        }

        viewModel.Click(row, e.KeyModifiers.HasFlag(KeyModifiers.Control), e.KeyModifiers.HasFlag(KeyModifiers.Shift));

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

        e.Handled = true;
    }

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
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

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
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
            viewModel.ResizeTo(_startWidth + delta.X, commit: true);
        }
        else if (started)
        {
            viewModel.MoveTo(_startX + delta.X, _startY + delta.Y, commit: true);
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => _gesture = Gesture.None;

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
        else if (tag == "add")
        {
            ShowAddPalette(viewModel.AddTargetOf(row));
        }

        e.Handled = true;
    }

    // Async void on purpose: a failed save reaches the UI thread's error handler instead of vanishing.
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel)
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

    /// <summary>The row whose template contains <paramref name="source"/>, or null (the resize strip, empty space).</summary>
    private static WorldRowViewModel? RowOf(Visual source) =>
        source.GetSelfAndVisualAncestors().OfType<Control>().Select(control => control.DataContext).OfType<WorldRowViewModel>().FirstOrDefault();
}
