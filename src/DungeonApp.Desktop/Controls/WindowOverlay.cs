using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Warstwa nad całą treścią okna (MainWindow.axaml, ostatnie dziecko głównego panelu): nosi dymki
/// powiadomień (<see cref="NotificationToast"/>) w prawym dolnym rogu i otwarte okno potwierdzenia
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
        Toasts.Bind(MarginProperty, Toasts.GetResourceObservable("DungeonPaddingLg"));
        Children.Add(Toasts);
    }

    /// <summary>Stos dymków: najnowszy na dole.</summary>
    internal StackPanel Toasts { get; }

    /// <summary>Warstwa okna, w którym stoi <paramref name="origin"/>; null, gdy okno jej nie ma.</summary>
    internal static WindowOverlay? Find(Visual origin) =>
        TopLevel.GetTopLevel(origin)?.GetVisualDescendants().OfType<WindowOverlay>().FirstOrDefault();
}
