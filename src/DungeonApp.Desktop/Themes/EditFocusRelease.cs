using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Konwencja interakcji ramy, raz dla całej aplikacji: kliknięcie poza polem, w którym trwa edycja,
/// i klawisz Escape w polu zdejmują z niego fokus - karetka przestaje migać, pole wraca do
/// spoczynku. To samo dotyczy tekstu do zaznaczenia (SelectableTextBlock): kliknięcie obok zdejmuje
/// z niego fokus, a z nim zaznaczenie. Zmienia wyłącznie fokus widoku, nigdy stan aplikacji.
/// </summary>
/// <remarks>
/// Obsługa klasowa na <see cref="TopLevel"/> obejmuje każde okno i każde okienko wysuwane (jego
/// PopupRoot też jest TopLevel), bez rejestracji w widokach. Kliknięcie przechodzi dalej
/// nieobsłużone, więc przycisk albo inne pole działa od pierwszego kliknięcia.
/// </remarks>
internal static class EditFocusRelease
{
    public static void Register()
    {
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPointerPressed, RoutingStrategies.Tunnel);
        InputElement.KeyDownEvent.AddClassHandler<TextBox>(OnTextBoxKeyDown);
    }

    private static void OnPointerPressed(TopLevel topLevel, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(topLevel).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var focused = topLevel.FocusManager?.GetFocusedElement();
        if (focused is not (TextBox or SelectableTextBlock))
        {
            return;
        }

        // Kliknięcie wciąż w tym samym polu (także w przycisku wewnątrz pola, np. czyszczenia)
        // nie przerywa edycji.
        if (e.Source is Visual source && IsInside(source, (Visual)focused))
        {
            return;
        }

        // Kliknięcie w menu (np. menu kontekstowe pola: wklej) działa na polu - fokus zostaje.
        if (e.Source is Visual menuSource && (menuSource is MenuBase || menuSource.FindAncestorOfType<MenuBase>() is not null))
        {
            return;
        }

        // Avalonia 12.0.5 zdejmuje fokus przez ustawienie fokusu na null; osobne ClearFocus jest nowsze.
        topLevel.FocusManager!.Focus(null, NavigationMethod.Pointer, e.KeyModifiers);
    }

    private static void OnTextBoxKeyDown(TextBox textBox, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || e.Handled)
        {
            return;
        }

        // Escape nie czyści tekstu - tylko kończy edycję.
        TopLevel.GetTopLevel(textBox)?.FocusManager?.Focus(null, NavigationMethod.Unspecified, e.KeyModifiers);
        e.Handled = true;
    }

    private static bool IsInside(Visual source, Visual focused)
    {
        return ReferenceEquals(source, focused) || focused.IsVisualAncestorOf(source);
    }
}
