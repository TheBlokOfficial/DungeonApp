using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DungeonApp.Desktop.Features.CampaignWorkspace;

namespace DungeonApp.Desktop.Shell;

public partial class AppShellView : UserControl
{
    private bool _startupStarted;

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

        try
        {
            // Loaded means the lightweight shell has completed its first layout/render. Only now do
            // we spend startup time on data and the critical campaign visual path.
            await viewModel.InitializeAsync();
            await WarmWorkspaceAsync(viewModel);
            viewModel.CompleteStartup();
        }
        catch (Exception)
        {
            // Startup optimization is never allowed to lock the user out. Repository failures have
            // their own messages; an unexpected warmup failure degrades to ordinary lazy loading.
            WarmupHost.Content = null;
            viewModel.CompleteStartupWithWarning();
        }
    }

    private async Task WarmWorkspaceAsync(AppShellViewModel viewModel)
    {
        var warmupViewModel = await viewModel.CreateWorkspaceWarmupAsync();
        Control warmup = warmupViewModel is null
            ? new WorkspaceWarmupView()
            : new CampaignWorkspaceView { DataContext = warmupViewModel };
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnWarmupLoaded(object? sender, RoutedEventArgs args) => loaded.TrySetResult();

        try
        {
            warmup.Loaded += OnWarmupLoaded;
            WarmupHost.Content = warmup;

            await loaded.Task;

            // Let work queued by Loaded/SizeChanged complete after the rendered warmup frame before
            // the tree is removed and navigation becomes interactive.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
        finally
        {
            warmup.Loaded -= OnWarmupLoaded;
            WarmupHost.Content = null;
            warmupViewModel?.Dispose();
        }
    }
}
