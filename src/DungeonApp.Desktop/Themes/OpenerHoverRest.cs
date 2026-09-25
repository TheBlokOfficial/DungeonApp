using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Konwencja interakcji ramy, raz dla całej aplikacji: to, co otworzyło okienko wysuwane albo
/// listę rozwijaną (przycisk, ComboBox, DropDownPicker), po zamknięciu kliknięciem w siebie
/// przechodzi w spoczynek i zostaje w nim, dopóki mysz z niego nie zjedzie - najechanie wraca
/// dopiero po ponownym wjechaniu. Kliknięcie w tym stanie otwiera normalnie.
/// </summary>
/// <remarks>
/// Przyczyna mignięcia (zmierzona w Avalonii 12.0.5, test OpenerHoverRestTests): otwarte okno
/// wyskakujące kładzie na oknie warstwę zamykania kliknięciem obok (LightDismissOverlayLayer), więc
/// otwierający traci :pointerover; wciśnięcie, które trafia w tę warstwę i zamyka okno, zdejmuje
/// :flyout-open (:dropdownopen), a :pointerover wraca dopiero z następnym zdarzeniem wskaźnika
/// (puszczenie albo ruch) - spoczynek na czas wciśnięcia, potem znów najechanie.
/// Obsługa klasowa na <see cref="TopLevel"/> dokłada przy tym wciśnięciu klasę
/// <see cref="SuppressedClass"/>; motyw ramy nie pokazuje najechania kontrolki z tą klasą
/// (<c>:pointerover:not(.hover-suppressed)</c>). Klasę zdejmuje zjechanie myszy z kontrolki albo
/// jej następne wciśnięcie. Zmienia wyłącznie wygląd widoku, nigdy stan aplikacji.
/// Zamknięcie Escape przy myszy stojącej nad otwierającym nie jest objęte: najechanie wraca
/// z następnym ruchem myszy. Otwarte jest naraz najwyżej jedno okienko - zapamiętany jest jeden
/// otwierający.
/// </remarks>
internal static class OpenerHoverRest
{
    public const string SuppressedClass = "hover-suppressed";

    private static Control? open;
    private static Control? suppressed;
    private static bool registered;

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        FlyoutBase.IsOpenProperty.Changed.AddClassHandler<FlyoutBase>((flyout, e) => OnOpenChanged(flyout.Target, e));
        ComboBox.IsDropDownOpenProperty.Changed.AddClassHandler<ComboBox>(OnOpenChanged);
        DropDownPicker.IsDropDownOpenProperty.Changed.AddClassHandler<DropDownPicker>(OnOpenChanged);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerExitedEvent.AddClassHandler<Control>(OnPointerExited, handledEventsToo: true);
    }

    private static void OnOpenChanged(Control? opener, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            open = opener;
        }
        else if (ReferenceEquals(opener, open))
        {
            open = null;
        }
    }

    // Wciśnięcie trafia w warstwę zamykania kliknięciem obok (LightDismissOverlayLayer), nie
    // w otwierającego, i ta obsługa biegnie przed zamknięciem - dlatego otwierający rozpoznaje się
    // po położeniu wciśnięcia w jego granicach, nie po źródle zdarzenia.
    private static void OnPointerPressed(TopLevel topLevel, PointerPressedEventArgs e)
    {
        Release();
        if (open is null || TopLevel.GetTopLevel(open) != topLevel)
        {
            return;
        }

        if (new Rect(open.Bounds.Size).Contains(e.GetPosition(open)))
        {
            suppressed = open;
            suppressed.Classes.Add(SuppressedClass);
        }
    }

    private static void OnPointerExited(Control control, PointerEventArgs e)
    {
        if (ReferenceEquals(control, suppressed))
        {
            Release();
        }
    }

    private static void Release()
    {
        suppressed?.Classes.Remove(SuppressedClass);
        suppressed = null;
    }
}
