using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Wspólna mechanika kroków rozgrzewki wizualnej: podepnij kontrolkę pod niewidoczny host, poczekaj
/// na jej <see cref="Control.Loaded"/>, oddaj sterowanie dispatcherowi, odepnij. Wydzielone, żeby
/// każdy krok per typ panelu nie powtarzał tej samej sekwencji.
/// </summary>
internal static class VisualWarmupHost
{
    public static async Task AttachAndWaitAsync(
        ContentControl host,
        Control control,
        CancellationToken cancellationToken)
    {
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnLoaded(object? sender, RoutedEventArgs args) => loaded.TrySetResult();

        try
        {
            control.Loaded += OnLoaded;
            host.Content = control;

            await loaded.Task.WaitAsync(cancellationToken).ConfigureAwait(true);

            // Let work queued by Loaded/SizeChanged complete after the rendered warmup frame before
            // the tree is removed and the next step takes the host.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
        finally
        {
            control.Loaded -= OnLoaded;
            host.Content = null;
        }
    }
}
