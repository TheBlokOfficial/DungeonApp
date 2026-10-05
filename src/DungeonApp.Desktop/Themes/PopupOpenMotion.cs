using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Flyout, dropdown and menu opening (every popup whose content is
/// FlyoutPresenter - Flyout, ComboBox, DropDownPicker - or MenuFlyoutPresenter, ContextMenu
/// and MenuItem submenu surface), applied once across the application:
/// <list type="bullet">
/// <item>determines whether the popup sits below or above the opener, and gives the opener
/// <see cref="OpensUpClass"/> when above - the theme rotates the dropdown arrow upwards;</item>
/// <item>fades the surface in and slides it by DungeonPopupOpenOffset from the opener's
/// side over DungeonPopupOpenDuration. Context menus and submenus have no opener
/// side - fade in only. Popup is open and clickable from the first frame -
/// motion only catches up with state. Closing is immediate (not handled here).</item>
/// </list>
/// System animations disabled: ReducedMotion.axaml sets <see cref="IsEnabledProperty"/>
/// to false and motion does not start; arrow direction still works.
/// </summary>
/// <remarks>
/// Class handling of Popup.IsOpen changes runs after popup opening, so its screen position
/// is already fixed. Changes view appearance only, never application state.
/// </remarks>
internal static class PopupOpenMotion
{
    public const string OpensUpClass = "opens-up";

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(PopupOpenMotion), defaultValue: true);

    private static bool registered;

    public static bool GetIsEnabled(Control presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

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
        if (e.NewValue is not true || popup.Child is not Control presenter)
        {
            return;
        }

        // Context menus and submenus: no opener side - fade only.
        if (presenter is ContextMenu || popup.TemplatedParent is MenuItem)
        {
            if (GetIsEnabled(presenter))
            {
                Run(presenter, opensUp: false, offset: 0d);
            }

            return;
        }

        if (presenter is not (FlyoutPresenter or MenuFlyoutPresenter))
        {
            return;
        }

        var target = popup.PlacementTarget ?? popup.Parent as Control;
        var popupTop = presenter.PointToScreen(default).Y;
        var targetTop = target?.PointToScreen(default).Y;
        var opensUp = targetTop is { } top && popupTop < top;

        // Opener: control whose template contains the popup (ComboBox, DropDownPicker), or flyout
        // target (button).
        var opener = popup.TemplatedParent as Control ?? target;
        opener?.Classes.Set(OpensUpClass, opensUp);

        if (GetIsEnabled(presenter))
        {
            Run(presenter, opensUp, offset: null);
        }
    }

    private static void Run(Control presenter, bool opensUp, double? offset)
    {
        offset ??= Offset(presenter);

        // Opens down - from above (negative offset), opens up - from below.
        Appear(presenter, opensUp ? offset.Value : -offset.Value);
    }

    /// <summary>DungeonPopupOpenOffset - distance the surface slides from the opener's side.</summary>
    internal static double Offset(Control control) =>
        control.TryFindResource("DungeonPopupOpenOffset", out var o) && o is double value ? value : 4d;

    /// <summary>
    /// Appearance motion shared by popups and expanded section content (Themes/ExpanderContentMotion.cs):
    /// fades in and moves from <paramref name="fromY"/> to its position over
    /// DungeonPopupOpenDuration, easing out. Blocks nothing - content is clickable from the first
    /// frame, motion only catches up with state.
    /// </summary>
    internal static void Appear(Control target, double fromY)
    {
        var duration = target.TryFindResource("DungeonPopupOpenDuration", out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(120);

        // Offset animation uses TranslateTransform in RenderTransform (Avalonia 12's transform animator
        // does not animate RenderTransform stored as TransformOperations).
        if (target.RenderTransform is not TranslateTransform)
        {
            target.RenderTransform = new TranslateTransform();
        }

        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0d),
                        new Setter(TranslateTransform.YProperty, fromY),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1d),
                        new Setter(TranslateTransform.YProperty, 0d),
                    },
                },
            },
        };

        _ = animation.RunAsync(target);
    }
}
