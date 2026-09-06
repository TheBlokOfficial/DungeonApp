using Avalonia.Controls;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Startup;

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
        // Loaded means the lightweight shell has completed its first layout/render. Only now do we
        // spend startup time on the registered sequence of data and visual warmup steps. The runner
        // owns its own failure handling - a step going wrong degrades to lazy loading, it never
        // blocks entry.
        await viewModel.RunStartupAsync(new StartupUiContext(WarmupHost));
    }

}
