using System;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Diagnostics;

/// <summary>
/// Where every exception the UI thread does not catch ends up: a GM action's command (a synchronous
/// one throws inside the click; an asynchronous one has its failure rethrown on the UI thread by the
/// command itself), an input or layout handler, a timer. At the table, closing the program costs
/// more than the error - the campaign is saved after every change - so the error goes to the log,
/// the GM gets one error notification, and the program keeps running.
/// <para>
/// The notification goes to the main window's overlay, never to the window of whatever threw: an
/// exception has no element to start from, and a popup's window has no overlay at all.
/// </para>
/// </summary>
public static class UiThreadErrors
{
    public const string NotificationMessage = "Nie udało się wykonać tej czynności. Szczegóły w logu.";

    /// <summary>
    /// The dispatcher's handler: keeps the program running, records the exception, and tells the GM
    /// in <paramref name="window"/> - or nowhere, while there is no window yet.
    /// </summary>
    public static void Handle(DispatcherUnhandledExceptionEventArgs e, Visual? window)
    {
        ArgumentNullException.ThrowIfNull(e);

        e.Handled = true;
        AppLog.Error("Błąd na wątku interfejsu; program działa dalej.", e.Exception);
        Notify(window);
    }

    private static void Notify(Visual? window)
    {
        if (window is null || WindowOverlay.Find(window) is not { } overlay)
        {
            return;
        }

        // One notification for any number of failures: an error that repeats on every frame would
        // otherwise bury the window under a growing stack of identical ones. A second notification
        // appears only once the GM has closed the first.
        if (overlay.Toasts.Children.OfType<NotificationToast>().Any(toast => toast.Message == NotificationMessage))
        {
            return;
        }

        try
        {
            NotificationToast.Show(window, NotificationKind.Error, NotificationMessage);
        }
        catch (Exception ex)
        {
            AppLog.Error("Nie udało się pokazać powiadomienia o błędzie.", ex);
        }
    }
}
