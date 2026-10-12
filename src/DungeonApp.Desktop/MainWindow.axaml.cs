using Avalonia;
using Avalonia.Controls;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop;

public partial class MainWindow : Window
{
    private readonly WindowPlacementStore? _placementStore;
    private Size _normalSize;
    private Size _previousNormalSize;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Restores the remembered state and size, and saves them when the window closes.</summary>
    public MainWindow(WindowPlacementStore placementStore) : this()
    {
        _placementStore = placementStore;

        if (placementStore.Load() is { } placement)
        {
            Width = placement.Width;
            Height = placement.Height;
            if (placement.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        _normalSize = _previousNormalSize = new Size(Width, Height);
        Closing += (_, _) => placementStore.Save(new WindowPlacement(
            WindowState == WindowState.Maximized, _normalSize.Width, _normalSize.Height));
    }

    // The size to return to after un-maximizing: only sizes seen while the window is in its normal
    // state count (Width/Height cannot be trusted to keep it while maximized).
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_placementStore is not null && WindowState == WindowState.Normal && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            _previousNormalSize = _normalSize;
            _normalSize = e.NewSize;
        }
    }

    // The platform may report the maximized size while the state still reads Normal; that size is
    // then the one just recorded, so the one before it is the real normal size.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty
            && change.GetNewValue<WindowState>() == WindowState.Maximized
            && _normalSize == ClientSize)
        {
            _normalSize = _previousNormalSize;
        }
    }
}
