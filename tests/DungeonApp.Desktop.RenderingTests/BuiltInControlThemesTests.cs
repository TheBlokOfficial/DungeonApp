using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Aplikacja nie ma motywu domyślnego biblioteki (porcja 9a) - każdy wbudowany typ kontrolki z
/// szablonem, którego używa, ma motyw ramy. Kontrolka bez motywu nie rysuje się wcale, a w rzadko
/// otwieranym miejscu (okienko, menu podręczne) nikt tego nie zauważy, dopóki tam nie kliknie.
/// Lista jawna: nowy wbudowany typ w widoku albo w szablonie dopisuje się tutaj.
/// Typy używane wyłącznie z motywem nazwanym (ToggleButton, RepeatButton, Thumb) tu nie należą -
/// ich motyw wskazuje miejsce użycia, nie typ.
/// </summary>
public sealed class BuiltInControlThemesTests
{
    private static readonly Type[] TemplatedTypesInUse =
    [
        // Infrastruktura: okno, hosty okienek, pojemniki.
        typeof(Window),
        typeof(PopupRoot),
        typeof(OverlayPopupHost),
        typeof(ItemsControl),
        typeof(TransitioningContentControl),
        typeof(PathIcon),
        // Okienka.
        typeof(FlyoutPresenter),
        typeof(MenuFlyoutPresenter),
        typeof(ContextMenu),
        typeof(MenuItem),
        typeof(ToolTip),
        // Kontrolki widoków.
        typeof(Button),
        typeof(TextBox),
        typeof(ButtonSpinner),
        typeof(NumericUpDown),
        typeof(ScrollViewer),
        typeof(ScrollBar),
        typeof(ListBox),
        typeof(ListBoxItem),
        typeof(Separator),
        typeof(CheckBox),
        typeof(RadioButton),
        typeof(ToggleSwitch),
        typeof(Slider),
        typeof(ProgressBar),
        typeof(ComboBox),
        typeof(ComboBoxItem),
        typeof(TabControl),
        typeof(TabItem),
        typeof(TabStrip),
        typeof(TabStripItem),
        typeof(Expander),
    ];

    [AvaloniaFact]
    public void Every_built_in_templated_control_in_use_has_a_frame_theme_with_a_template()
    {
        var missing = TemplatedTypesInUse
            .Where(type => !HasThemeWithTemplate(type))
            .Select(type => type.Name)
            .ToList();

        Assert.Empty(missing);
    }

    private static bool HasThemeWithTemplate(Type type) =>
        Application.Current!.TryFindResource(type, out var resource)
        && resource is ControlTheme theme
        && SetsTemplate(theme);

    // Szablon bywa w motywie bazowym (ComboBoxItem na ListBoxItem) albo w stylu motywu
    // (ScrollBar: osobny szablon dla :vertical i :horizontal).
    private static bool SetsTemplate(StyleBase style) =>
        style.Setters.OfType<Setter>().Any(setter => setter.Property == TemplatedControl.TemplateProperty)
        || style.Children.OfType<StyleBase>().Any(SetsTemplate)
        || (style is ControlTheme { BasedOn: { } basedOn } && SetsTemplate(basedOn));
}
