using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Warstwa nad całą treścią okna (MainWindow.axaml, ostatnie dziecko głównego panelu): nosi dymki
/// powiadomień (<see cref="NotificationToast"/>) w prawym dolnym rogu nad paskiem stanu i otwarte okno potwierdzenia
/// (<see cref="ConfirmationDialog"/>) nad wszystkim. Pusta nie ma tła, więc nie łapie myszy - klik
/// trafia w treść pod nią. Widoki jej nie znają: znajdują ją metody pokazujące przez element, z
/// którego je wywołano (<see cref="Find"/>).
/// </summary>
public sealed class WindowOverlay : Panel
{
    public WindowOverlay()
    {
        Toasts = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        Toasts.Bind(StackPanel.SpacingProperty, Toasts.GetResourceObservable("DungeonSpacingSm"));
        Toasts.GetResourceObservable("DungeonPaddingLg").Subscribe(new AnonymousObserver<object?>(value =>
        {
            toastGap = value is Thickness gap ? gap : default;
            UpdateToastMargin();
        }));
        Toasts.GetResourceObservable("DungeonStatusBarHeight").Subscribe(new AnonymousObserver<object?>(value =>
        {
            statusBarHeight = value is GridLength height ? height.Value : 0;
            UpdateToastMargin();
        }));
        Children.Add(Toasts);
    }

    private Thickness toastGap;
    private double statusBarHeight;

    /// <summary>Stos dymków: najnowszy na dole.</summary>
    internal StackPanel Toasts { get; }

    /// <summary>Warstwa okna, w którym stoi <paramref name="origin"/>; null, gdy okno jej nie ma.</summary>
    internal static WindowOverlay? Find(Visual origin) =>
        TopLevel.GetTopLevel(origin)?.GetVisualDescendants().OfType<WindowOverlay>().FirstOrDefault();

    /// <summary>
    /// Stos stoi nad paskiem stanu (DungeonStatusBarHeight, Shell/AppShellView.axaml) z tym samym
    /// odstępem DungeonPaddingLg, jaki ma od prawej krawędzi okna - dymek nie zasłania paska.
    /// </summary>
    private void UpdateToastMargin() =>
        Toasts.Margin = new Thickness(toastGap.Left, toastGap.Top, toastGap.Right, toastGap.Bottom + statusBarHeight);
}
