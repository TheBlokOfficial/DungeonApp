using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Rozwinięcie sekcji (Expander), raz dla całej aplikacji: treść rozwiniętej sekcji wyłania się
/// jak okienko - z przezroczystości,
/// dosuwając się o DungeonPopupOpenOffset od strony nagłówka (z góry), w DungeonPopupOpenDuration,
/// z wyhamowaniem (ten sam ruch co otwarcie okienka, <see cref="PopupOpenMotion.Appear"/>).
/// Wysokość się nie animuje - to, co pod sekcją, przesuwa się od razu; treść jest klikalna od
/// pierwszej klatki; zwinięcie jest natychmiastowe (nic tu go nie dotyczy).
/// Wyłączone animacje w systemie (<see cref="SystemMotion.IsReduced"/>) - ruch się nie uruchamia.
/// </summary>
/// <remarks>
/// Rusza się prezenter treści z szablonu sekcji (PART_ContentPresenter). Sekcja rozwinięta już przy
/// budowie nie ma jeszcze szablonu - pojawia się bez ruchu. Zmienia wyłącznie wygląd widoku, nigdy
/// stan aplikacji.
/// </remarks>
internal static class ExpanderContentMotion
{
    private static bool registered;

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        Expander.IsExpandedProperty.Changed.AddClassHandler<Expander>(OnIsExpandedChanged);
    }

    private static void OnIsExpandedChanged(Expander expander, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || SystemMotion.IsReduced)
        {
            return;
        }

        var content = expander.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .FirstOrDefault(presenter => presenter.Name == "PART_ContentPresenter"
                                         && presenter.TemplatedParent == expander);
        if (content is not null)
        {
            PopupOpenMotion.Appear(content, -PopupOpenMotion.Offset(content));
        }
    }
}
