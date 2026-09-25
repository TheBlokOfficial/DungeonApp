using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Konwencja interakcji ramy, raz dla całej aplikacji: ikona na początku pola tekstowego
/// (InnerLeftContent - ołówek, lupa) stoi obok obszaru tekstu, nie w nim. Wciśnięcie na niej nie
/// stawia karetki, nie zaczyna zaznaczania i nie przenosi fokusu - ikona nie reaguje na mysz wcale.
/// Zmienia wyłącznie obsługę wskaźnika w widoku, nigdy stan aplikacji.
/// </summary>
/// <remarks>
/// Pole obsługuje wciśnięcie w fazie bąbelkowej, a fokus przy kliknięciu przenosi obsługa tunelowa
/// przy samym źródle; obie pomijają zdarzenie obsłużone wcześniej w tunelu, na polu. Przycisk
/// czyszczenia i strzałki pola liczbowego tego nie potrzebują - są przyciskami, które same obsługują
/// wciśnięcie.
/// </remarks>
internal static class FieldIconPointer
{
    private const string LeftContentPartName = "PART_InnerLeftContent";

    public static void Register()
    {
        InputElement.PointerPressedEvent.AddClassHandler<TextBox>(OnTextBoxPointerPressed, RoutingStrategies.Tunnel);
    }

    private static void OnTextBoxPointerPressed(TextBox textBox, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source)
        {
            return;
        }

        var onIcon = source.GetSelfAndVisualAncestors()
            .OfType<ContentPresenter>()
            .Any(presenter => presenter.Name == LeftContentPartName && ReferenceEquals(presenter.TemplatedParent, textBox));
        if (onIcon)
        {
            e.Handled = true;
        }
    }
}
