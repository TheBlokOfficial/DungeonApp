using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Shell.Sidebars;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop.Shell;

public partial class AppShellView : UserControl
{
    private bool _startupStarted;
    private GlobalSidebarViewModel? _sidebar;
    private CancellationTokenSource? _sidebarAnimationCancellation;

    public AppShellView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_startupStarted || DataContext is not AppShellViewModel viewModel)
        {
            return;
        }

        _startupStarted = true;
        AttachSidebar(viewModel);

        // Loaded means the lightweight shell has completed its first layout/render. Only now do we
        // spend startup time on the registered sequence of data and visual warmup steps. The runner
        // owns its own failure handling - a step going wrong degrades to lazy loading, it never
        // blocks entry.
        await viewModel.RunStartupAsync(new StartupUiContext(WarmupHost));
    }

    private void AttachSidebar(AppShellViewModel viewModel)
    {
        if (ReferenceEquals(_sidebar, viewModel.Sidebar))
        {
            return;
        }

        if (_sidebar is not null)
        {
            _sidebar.PropertyChanged -= OnSidebarPropertyChanged;
        }

        _sidebar = viewModel.Sidebar;
        _sidebar.PropertyChanged += OnSidebarPropertyChanged;
        SetSidebarWidth(_sidebar.SidebarWidth.Value);
    }

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GlobalSidebarViewModel.SidebarWidth) && _sidebar is not null)
        {
            AnimateSidebarWidth(_sidebar.SidebarWidth.Value);
        }
    }

    private async void AnimateSidebarWidth(double targetWidth)
    {
        _sidebarAnimationCancellation?.Cancel();
        _sidebarAnimationCancellation?.Dispose();
        _sidebarAnimationCancellation = new CancellationTokenSource();
        var cancellationToken = _sidebarAnimationCancellation.Token;
        var sourceWidth = SidebarColumn.Width.Value;
        var stopwatch = Stopwatch.StartNew();
        const double durationMilliseconds = 180;

        try
        {
            while (stopwatch.Elapsed.TotalMilliseconds < durationMilliseconds)
            {
                var progress = stopwatch.Elapsed.TotalMilliseconds / durationMilliseconds;
                SetSidebarWidth(sourceWidth + ((targetWidth - sourceWidth) * EaseInOutCubic(progress)));
                await Task.Delay(16, cancellationToken);
            }

            SetSidebarWidth(targetWidth);
        }
        catch (OperationCanceledException)
        {
            // A new toggle continues smoothly from the width reached by this animation.
        }
    }

    private void SetSidebarWidth(double width)
    {
        SidebarColumn.Width = new GridLength(width);
        if (DataContext is AppShellViewModel viewModel)
        {
            viewModel.StatusBar.SidebarWidth = new GridLength(width);
        }
    }

    private static double EaseInOutCubic(double progress) => progress < 0.5
        ? 4 * progress * progress * progress
        : 1 - (Math.Pow((-2 * progress) + 2, 3) / 2);

    private ColumnDefinition SidebarColumn => ShellLayout.ColumnDefinitions[0];
}
