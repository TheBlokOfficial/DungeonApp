using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Diagnostics;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// A GM action that throws must not end the program: the exception reaches the log, the GM gets one
/// error notification, and the same button works again. The test attaches the same handler the
/// desktop App attaches - App does not attach it under the headless test platform, where an
/// exception has to keep failing the test.
/// </summary>
public sealed class UiThreadErrorsTests
{
    [AvaloniaFact]
    public void A_throwing_async_command_is_logged_notified_and_leaves_the_button_working()
    {
        var runs = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            runs++;
            await Task.Yield();
            throw new InvalidOperationException("komenda zawiodła " + runs);
        });
        var button = new Button { Content = "Akcja", Command = command };

        RunWithHandler(button, (window, overlay, logged) =>
        {
            Click(window, button);

            Assert.Equal(1, runs);
            var entry = Assert.Single(logged);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Equal("komenda zawiodła 1", entry.Exception?.Message);

            var toast = Assert.Single(overlay.Toasts.Children.OfType<NotificationToast>());
            Assert.Equal(NotificationKind.Error, toast.Kind);
            Assert.Equal(UiThreadErrors.NotificationMessage, toast.Message);

            Assert.True(window.IsVisible);
            Assert.True(command.CanExecute(null));

            Click(window, button);

            Assert.Equal(2, runs);
            Assert.Equal(2, logged.Count);
            Assert.Single(overlay.Toasts.Children.OfType<NotificationToast>());
        });
    }

    [AvaloniaFact]
    public void A_throwing_sync_command_on_the_ui_thread_is_logged_and_notified()
    {
        var command = new RelayCommand(() => throw new InvalidOperationException("komenda zawiodła"));
        var button = new Button { Content = "Akcja", Command = command };

        RunWithHandler(button, (window, overlay, logged) =>
        {
            // On Windows a click reaches the button inside a dispatcher job, which is what routes a
            // synchronous throw to the handler; headless input does not, so the job is posted here.
            Dispatcher.UIThread.Post(() => command.Execute(null));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("komenda zawiodła", Assert.Single(logged).Exception?.Message);
            Assert.Single(overlay.Toasts.Children.OfType<NotificationToast>());
            Assert.True(window.IsVisible);
        });
    }

    private static void RunWithHandler(
        Control content,
        Action<Window, WindowOverlay, List<(LogLevel Level, Exception? Exception)>> body)
    {
        var overlay = new WindowOverlay();
        var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { content, overlay } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var logged = new List<(LogLevel Level, Exception? Exception)>();
        var previousSink = AppLog.Use((level, _, exception) => logged.Add((level, exception)));
        DispatcherUnhandledExceptionEventHandler handler = (_, e) => UiThreadErrors.Handle(e, window);
        Dispatcher.UIThread.UnhandledException += handler;

        try
        {
            body(window, overlay, logged);
        }
        finally
        {
            Dispatcher.UIThread.UnhandledException -= handler;
            AppLog.Use(previousSink);
            window.Close();
        }
    }

    private static void Click(Window window, Control target)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);

        // The command's own continuation, then the rethrow it posts back to the UI thread.
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }
}
