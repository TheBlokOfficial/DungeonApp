using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
        var curtainFill = CurtainProgressFill();
        await viewModel.RunStartupAsync(new StartupUiContext(WarmupHost)
        {
            WaitForFrameAsync = WaitForFrameAsync,
            WaitForProgressSettledAsync = () => curtainFill?.WhenSettledAsync() ?? Task.CompletedTask,
            ProgressSettleLimit = curtainFill?.SettleLimit ?? TimeSpan.Zero,
        });
    }

    /// <summary>
    /// Without a frame between steps the next step can take the UI thread before the bar has drawn
    /// its new target even once. Bounded, because a minimized window draws no frames.
    /// </summary>
    private Task WaitForFrameAsync()
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return Task.CompletedTask;
        }

        var drawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        topLevel.RequestAnimationFrame(_ => drawn.TrySetResult());
        return Task.WhenAny(drawn.Task, Task.Delay(FrameWaitLimit));
    }

    private static readonly TimeSpan FrameWaitLimit = TimeSpan.FromMilliseconds(100);

    /// <summary>The startup curtain is the window's (MainWindow.axaml), so its bar is found by name there.</summary>
    private ProgressFill? CurtainProgressFill() =>
        TopLevel.GetTopLevel(this) is { } topLevel && NameScope.GetNameScope(topLevel) is { } names
            ? names.Find<ProgressBar>("StartupProgress")?.GetVisualDescendants().OfType<ProgressFill>().FirstOrDefault()
            : null;

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
