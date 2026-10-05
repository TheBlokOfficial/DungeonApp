using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The application has no library-default theme: every built-in templated control type it uses
/// has a frame theme. A control without a theme draws nothing, which can go unnoticed in a rarely
/// opened location (a popup or context menu) until someone clicks there.
/// Explicit list: add each new built-in type used in a view or template here.
/// Types used only with a named theme (ToggleButton, RepeatButton, Thumb) do not belong here;
/// their usage site selects the theme rather than their type.
/// </summary>
public sealed class BuiltInControlThemesTests
{
    private static readonly Type[] TemplatedTypesInUse =
    [
        // Infrastructure: window, popup hosts, containers.
        typeof(Window),
        typeof(PopupRoot),
        typeof(OverlayPopupHost),
        typeof(ItemsControl),
        typeof(TransitioningContentControl),
        typeof(PathIcon),
        // Popups.
        typeof(FlyoutPresenter),
        typeof(MenuFlyoutPresenter),
        typeof(ContextMenu),
        typeof(MenuItem),
        typeof(ToolTip),
        // View controls.
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

    // The template may live in the base theme (ComboBoxItem on ListBoxItem) or in a theme style
    // (ScrollBar: separate templates for :vertical and :horizontal).
    private static bool SetsTemplate(StyleBase style) =>
        style.Setters.OfType<Setter>().Any(setter => setter.Property == TemplatedControl.TemplateProperty)
        || style.Children.OfType<StyleBase>().Any(SetsTemplate)
        || (style is ControlTheme { BasedOn: { } basedOn } && SetsTemplate(basedOn));
}
