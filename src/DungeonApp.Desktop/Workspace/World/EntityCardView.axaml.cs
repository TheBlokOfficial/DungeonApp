using Avalonia.Controls;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>The window body of an entity's card; see <see cref="EntityCardViewModel"/>.</summary>
public partial class EntityCardView : UserControl
{
    // The card's margin on each side (EntityCardView.axaml). Equal on both sides so the card reads
    // centred in the window; the right one is wide enough to hold the overlay scrollbar with a clear
    // gap before the card, so the bar never needs room of its own.
    private const double CardSideMargin = 32;

    /// <summary>
    /// The width of the window's content: the card with its margins. The window adds its own frame,
    /// so a window of this width has no empty strip beside the card.
    /// </summary>
    public const double ContentWidth = CardSideMargin + Entries.Controls.EntryDetailView.CardWidth + CardSideMargin;

    public EntityCardView()
    {
        InitializeComponent();
    }
}
