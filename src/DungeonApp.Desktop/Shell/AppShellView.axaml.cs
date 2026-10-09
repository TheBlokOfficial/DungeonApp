using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Controls;
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
        viewModel.SaveFailed += ShowSaveFailure;
        // Loaded means the lightweight shell has completed its first layout/render. Only now do we
        // spend startup time on the registered sequence of data and visual warmup steps. The runner
        // owns its own failure handling - a step going wrong degrades to lazy loading, it never
        // blocks entry.
        await viewModel.RunStartupAsync(new StartupUiContext(WarmupHost));
    }

    /// <summary>
    /// One notification for any number of failed saves: while the same warning still hangs in the
    /// window, a repeat adds nothing. Shown through this view's own window, which is where the
    /// overlay is (a popup's window has none).
    /// </summary>
    private void ShowSaveFailure(string warning)
    {
        if (WindowOverlay.Find(this) is not { } overlay ||
            overlay.Toasts.Children.OfType<NotificationToast>().Any(toast => toast.Message == warning))
        {
            return;
        }

        NotificationToast.Show(this, NotificationKind.Error, warning);
    }

}
