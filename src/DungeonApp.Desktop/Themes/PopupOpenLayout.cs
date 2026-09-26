using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Układ okienka ustalany przy otwarciu i niezmienny do zamknięcia (docs/architecture.md,
/// "Niezmiennik interfejsu", Pasek przewijania jest nakładką):
/// <list type="bullet">
/// <item>strefa paska przewijania w okienku (menu, menu kontekstowe, podmenu, ComboBox,
/// DropDownPicker, okienko .list-host) jest tylko wtedy, gdy treść się nie mieści - okienko, którego
/// treść mieści się bez przewijania, dostaje klasę <see cref="NoBarZoneClass"/>, a style
/// w BuiltInControls.axaml zdejmują po niej strefę z wierszy;</item>
/// <item>szerokość okienka listy (ComboBox, DropDownPicker) bierze się z najdłuższej pozycji całej
/// listy, nie z wierszy, które akurat istnieją (wirtualizacja tworzy tylko widoczne): pomiar
/// napisów pismem wiersza plus to, co wiersz i okienko dokładają wokół napisu. Nie mniej niż
/// otwierający, nie dalej niż do prawej krawędzi okna aplikacji; dłuższe pozycje się przycinają
/// (podpowiedź pełnego napisu - TrimmedLabelToolTip).</item>
/// </list>
/// Wyszukiwanie ani przewijanie nie zmienia ani strefy, ani szerokości otwartego okienka.
/// </summary>
/// <remarks>
/// Klasa trafia tam, skąd style ją widzą w drzewie logicznym wierszy: na powierzchnię okienka
/// (wiersze menu, wiersze DropDownPicker, lista w .list-host) i na kontrolkę, której szablon zawiera
/// okno (ComboBox - jego wiersze są jego dziećmi logicznymi; MenuItem - wiersze podmenu są jego
/// dziećmi). Obsługa zmiany Popup.IsOpen biegnie po pierwszym układzie okna, zanim okno się narysuje
/// (ruch otwarcia i tak zaczyna od przezroczystości). Zmienia wyłącznie wygląd widoku.
/// </remarks>
internal static class PopupOpenLayout
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

        var isOpen = e.NewValue is true;
        var noZone = isOpen && !Overflows(surface);
        surface.Classes.Set(NoBarZoneClass, noZone);
        if (owner is ComboBox or MenuItem)
        {
            owner.Classes.Set(NoBarZoneClass, noZone);
        }

        if (surface is FlyoutPresenter presenter && owner is ComboBox or DropDownPicker)
        {
            if (isOpen)
            {
                FixListWidth(presenter, owner);
            }
            else
            {
                presenter.ClearValue(Avalonia.Layout.Layoutable.WidthProperty);
            }
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

    private static void FixListWidth(FlyoutPresenter presenter, Control owner)
    {
        presenter.UpdateLayout();
        var row = presenter.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault(item => item.IsVisible);
        var label = row?.GetVisualDescendants().OfType<TextBlock>().LastOrDefault(text => !string.IsNullOrEmpty(text.Text));
        if (row is null || label is null)
        {
            return;
        }

        var typeface = new Typeface(label.FontFamily, label.FontStyle, label.FontWeight);
        double Measure(string text)
        {
            using var layout = new TextLayout(text, typeface, label.FontSize, null);
            return layout.WidthIncludingTrailingWhitespace;
        }

        var names = owner is DropDownPicker picker ? picker.ItemNames() : ComboBoxNames((ComboBox)owner);
        var longest = names.Select(Measure).DefaultIfEmpty(0d).Max();

        // Wokół napisu: wcięcia wiersza, pole wyboru, strefa paska (z wiersza, który istnieje), wcięcie
        // i krawędź okienka (różnica szerokości okienka i wiersza).
        var aroundText = row.DesiredSize.Width - Measure(label.Text!) + (presenter.Bounds.Width - row.Bounds.Width);
        var width = Math.Max(Math.Ceiling(longest + aroundText), owner.Bounds.Width);

        if (TopLevel.GetTopLevel(owner) is { } topLevel && owner.TranslatePoint(default, topLevel) is { } origin)
        {
            var room = topLevel.Bounds.Width - origin.X - presenter.Margin.Bottom;
            width = Math.Max(owner.Bounds.Width, Math.Min(width, room));
        }

        presenter.Width = width;
    }

    private static IEnumerable<string> ComboBoxNames(ComboBox combo)
    {
        TextBlock? evaluator = null;
        foreach (var item in combo.Items)
        {
            if (item is ContentControl container)
            {
                yield return container.Content?.ToString() ?? string.Empty;
            }
            else if (combo.DisplayMemberBinding is { } binding)
            {
                if (evaluator is null)
                {
                    evaluator = new TextBlock();
                    evaluator.Bind(TextBlock.TextProperty, binding);
                }

                evaluator.DataContext = item;
                yield return evaluator.Text ?? string.Empty;
            }
            else
            {
                yield return item?.ToString() ?? string.Empty;
            }
        }
    }
}
