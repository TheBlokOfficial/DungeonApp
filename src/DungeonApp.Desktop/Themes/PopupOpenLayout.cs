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
/// Popup layout fixed on opening and unchanged until closing (scrollbar is
/// an overlay):
/// <list type="bullet">
/// <item>popup scrollbar zone (menu, context menu, submenu, ComboBox,
/// DropDownPicker, .list-host flyout) exists only when content overflows - a popup whose
/// content fits without scrolling gets <see cref="NoBarZoneClass"/>, and styles
/// in BuiltInControls.axaml remove the zone from its rows;</item>
/// <item>list popup width (ComboBox, DropDownPicker) comes from the longest item in the entire
/// list, not currently existing rows (virtualisation creates only visible ones): measure
/// labels in the row font plus what the row and popup add around them. At least as wide as
/// the opener, no further than the application window's right edge; longer items trim
/// (full-text tooltip - TrimmedLabelToolTip).</item>
/// </list>
/// Searching and scrolling change neither zone nor width of an open popup.
/// </summary>
/// <remarks>
/// Class is set where styles can see it in the rows' logical tree: on the popup surface
/// (menu rows, DropDownPicker rows, .list-host list) and the control whose template contains
/// the popup (ComboBox - rows are its logical children; MenuItem - submenu rows are its
/// children). Popup.IsOpen handling runs after the first popup layout, before it is drawn
/// (opening motion starts transparent anyway). Changes view appearance only.
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

    // One popup layer scrolls: its own scroll area or the list in .list-host.
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

        // Around the label: row padding, checkbox, scrollbar zone (from an existing row), popup padding
        // and border (difference between popup and row widths).
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
