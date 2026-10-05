using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Shell interaction convention applied once across the application: a flyout or dropdown
/// opener (button, ComboBox, DropDownPicker), when clicked to close itself,
/// returns to rest until the pointer leaves - hover returns
/// only on re-entry. Clicking in this state opens normally.
/// </summary>
/// <remarks>
/// Flicker mechanism (Avalonia 12.0.5, OpenerHoverRestTests): an open popup
/// adds a light-dismiss layer (LightDismissOverlayLayer) to the window, so
/// the opener loses :pointerover; a press hitting that layer closes the popup and removes
/// :flyout-open (:dropdownopen), while :pointerover returns only with the next pointer event
/// (release or movement) - rest during the press, then hover again.
/// Class handling on <see cref="TopLevel"/> adds
/// <see cref="SuppressedClass"/> on that press; the shell theme hides hover for a control with this class
/// (<c>:pointerover:not(.hover-suppressed)</c>). Pointer exit or the next press
/// removes the class. Changes view appearance only, never application state.
/// Escape dismissal with the pointer over the opener is not covered: hover returns
/// on the next pointer movement. At most one popup is open at a time - one opener
/// is remembered.
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

    // The press hits the light-dismiss layer (LightDismissOverlayLayer), not
    // the opener, and this handler runs before dismissal - identify the opener
    // by the press position within its bounds, not by the event source.
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
