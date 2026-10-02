using Avalonia.Controls;
using DungeonApp.Core.Entries;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// The one entry-detail block: an entry's header - the breadcrumb trail, the name, the system's
/// tags - over the system's own card, 560 wide; or, for a broken
/// row or a rejected pack, the same header shape in the broken-content color over the full reason
/// and the file path. Its <see cref="Avalonia.StyledElement.DataContext"/> is a
/// <see cref="ContentTab.ContentDetailViewModel"/>. It draws no background, border, scroll
/// or outer margin - whoever hosts it (a content tab today, a campaign preview later) supplies those.
/// </summary>
public partial class EntryDetailView : UserControl
{
    public EntryDetailView()
    {
        InitializeComponent();
    }
}
