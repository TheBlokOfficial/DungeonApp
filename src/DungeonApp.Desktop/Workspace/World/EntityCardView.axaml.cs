using Avalonia.Controls;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>The window body of an entity's card; see <see cref="EntityCardViewModel"/>.</summary>
public partial class EntityCardView : UserControl
{
    // The view's own layout (EntityCardView.axaml): the card's left margin and the scrollbar zone
    // (DungeonScrollBarZone) after it.
    private const double CardLeftMargin = 24;
    private const double ScrollBarZone = 16;

    /// <summary>
    /// The width of the window's content: the card with the room this view gives it. The window adds
    /// its own frame, so a window of this width has no empty strip beside the card.
    /// </summary>
    public const double ContentWidth = CardLeftMargin + Entries.Controls.EntryDetailView.CardWidth + ScrollBarZone;

    public EntityCardView()
    {
        InitializeComponent();
    }
}
