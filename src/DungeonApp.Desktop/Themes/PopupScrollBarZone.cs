using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Strefa paska przewijania w okienku (menu, menu kontekstowe, podmenu, ComboBox, DropDownPicker,
/// okienko .list-host) jest tylko wtedy, gdy treść się nie mieści (docs/architecture.md,
/// "Niezmiennik interfejsu", Pasek przewijania jest nakładką): przy otwarciu okienka, którego treść
/// mieści się bez przewijania, daje klasę <see cref="NoBarZoneClass"/> - style w
/// BuiltInControls.axaml zdejmują po niej strefę paska z wierszy. Klasa zostaje do zamknięcia -
/// wyszukiwanie, które skróci listę, nie zmienia układu otwartego okienka.
/// </summary>
/// <remarks>
/// Klasa trafia tam, skąd style ją widzą w drzewie logicznym wierszy: na powierzchnię okienka
/// (wiersze menu, wiersze DropDownPicker, lista w .list-host) i na kontrolkę, której szablon zawiera
/// okno (ComboBox - jego wiersze są jego dziećmi logicznymi; MenuItem - wiersze podmenu są jego
/// dziećmi). Obsługa zmiany Popup.IsOpen biegnie po pierwszym układzie okna, zanim okno się narysuje
/// (ruch otwarcia i tak zaczyna od przezroczystości). Zmienia wyłącznie wygląd widoku.
/// </remarks>
internal static class PopupScrollBarZone
{
    public const string NoBarZoneClass = "no-bar-zone";

    private static bool registered;

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>(OnIsOpenChanged);
    }

    private static void OnIsOpenChanged(Popup popup, AvaloniaPropertyChangedEventArgs e)
    {
        if (popup.Child is not Control surface)
        {
            return;
        }

        var owner = popup.TemplatedParent as Control;
        var isMenuOrList = surface is FlyoutPresenter or MenuFlyoutPresenter or ContextMenu
                           || owner is MenuItem;
        if (!isMenuOrList)
        {
            return;
        }

        var noZone = e.NewValue is true && !Overflows(surface);
        surface.Classes.Set(NoBarZoneClass, noZone);
        if (owner is ComboBox or MenuItem)
        {
            owner.Classes.Set(NoBarZoneClass, noZone);
        }
    }

    // Przewija któraś warstwa okienka: jego własny obszar przewijania albo lista w .list-host.
    private static bool Overflows(Control surface)
    {
        surface.UpdateLayout();
        return surface.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .Where(viewer => viewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
            .Any(viewer => viewer.Extent.Height > viewer.Viewport.Height + 0.5);
    }
}
