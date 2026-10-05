using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Section expansion (Expander), applied once across the application: expanded content appears
/// like a popup - fading in,
/// sliding by DungeonPopupOpenOffset from the header side (above), over DungeonPopupOpenDuration,
/// easing out (same motion as popup opening, <see cref="PopupOpenMotion.Appear"/>).
/// Height is not animated - content below the section moves immediately; expanded content is clickable from
/// the first frame; collapse is immediate (not handled here).
/// System animations disabled (<see cref="SystemMotion.IsReduced"/>) - motion does not start.
/// </summary>
/// <remarks>
/// Moves the section template's content presenter (PART_ContentPresenter). A section expanded during
/// construction has no template yet - appears without motion. Changes view appearance only, never
/// application state.
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
