using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Shell interaction convention applied once across the application: clicking outside an edited field
/// or pressing Escape inside removes its focus - the caret stops blinking, the field returns to
/// rest. The same applies to selectable text (SelectableTextBlock): clicking outside removes
/// focus and selection. Changes view focus only, never application state.
/// </summary>
/// <remarks>
/// Class handling on <see cref="TopLevel"/> covers every window and flyout (its
/// PopupRoot is also a TopLevel), without registration in views. The click continues
/// unhandled, so a button or another field works on the first click.
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

        // Clicking within the same field (including an inner button, e.g. clear) does not
        // interrupt editing. A field composed by a control template (e.g. numeric field with arrows) counts
        // as a whole - its boundary is the control whose template contains it.
        // An icon field (IconField) counts as a whole: icon, frame border and clear button
        // belong to the field even though they sit outside the text box.
        var field = ((Visual)focused).FindAncestorOfType<IconField>()
                    ?? (focused as StyledElement)?.TemplatedParent as Visual
                    ?? (Visual)focused;
        if (e.Source is Visual source && IsInside(source, field))
        {
            return;
        }

        // Clicking a menu (e.g. the field's context menu: paste) acts on the field - focus stays.
        if (e.Source is Visual menuSource && (menuSource is MenuBase || menuSource.FindAncestorOfType<MenuBase>() is not null))
        {
            return;
        }

        // Avalonia 12.0.5 removes focus by setting it to null; a separate ClearFocus requires a newer version.
        topLevel.FocusManager!.Focus(null, NavigationMethod.Pointer, e.KeyModifiers);
    }

    private static void OnTextBoxKeyDown(TextBox textBox, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || e.Handled)
        {
            return;
        }

        // Escape does not clear text - only ends editing.
        TopLevel.GetTopLevel(textBox)?.FocusManager?.Focus(null, NavigationMethod.Unspecified, e.KeyModifiers);
        e.Handled = true;
    }

    private static bool IsInside(Visual source, Visual focused)
    {
        return ReferenceEquals(source, focused) || focused.IsVisualAncestorOf(source);
    }
}
