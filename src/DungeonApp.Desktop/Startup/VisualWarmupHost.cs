using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Startup;

/// <summary>
/// Shared visual warmup mechanics: attach a control to the invisible host, wait
/// for its <see cref="Control.Loaded"/>, yield to the dispatcher, detach. Shared so
/// each panel-type step does not repeat the same sequence.
/// <para>
/// Public, not <c>internal</c>: a system-provided startup step lives in another assembly and
/// needs the same sequence to warm up cards whose content only it knows.
/// </para>
/// </summary>
public static class VisualWarmupHost
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
