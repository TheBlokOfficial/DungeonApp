using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.Controls;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// The condition card. <see cref="Dnd5eSystem.CreateCard"/> sets the model once, immediately after
/// construction, through <see cref="SetCondition"/>: the constructor stays parameterless, as the XAML
/// loader and the designer preview need it.
/// <para>
/// Two pieces are lent to the detail header (<see cref="IEntryCardHeader"/>): the condition's icon
/// in the same square frame as a gear item's picture (DungeonDetailPictureWidth), drawn exactly as
/// that item's placeholder icon is - its shape, in the frame's dimmed color, never in the file's
/// own; and the summary, under the name. The card itself is the rules. Nothing on it is computed.
/// </para>
/// </summary>
public partial class ConditionCardView : UserControl, IEntryCardHeader
{
    private readonly ImageFrame _icon;

    private readonly SelectableTextBlock _summary = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Classes = { "body", "secondary" },
    };

    public ConditionCardView()
    {
        InitializeComponent();

        var side = ThemeResource.Get<double>("DungeonDetailPictureWidth");
        _icon = new ImageFrame
        {
            Width = side,
            Height = side,
            VerticalAlignment = VerticalAlignment.Top,
            // Sharp, like every bordered block on a card.
            CornerRadius = default,
            Icon = ThemeResource.Get<DrawingImage>("DungeonIconZap"),
        };
    }

    public Control HeaderVisual => _icon;

    /// <summary>Nothing at the end of the title's line: a condition's name has the line to itself.</summary>
    public Control? HeaderTitleEnd => null;

    /// <summary>Nothing before the tags: a condition has none.</summary>
    public Control? HeaderTagsStart => null;

    /// <summary>The summary, under the name.</summary>
    public Control HeaderBlock => _summary;

    public void SetCondition(StatusCondition condition, EntryPicture picture)
    {
        picture.ShowIn(_icon);

        _summary.Text = condition.Summary;

        RulesSection.Intro = condition.Rules.Intro;
        RulesSection.Items = [.. condition.Rules.Entries.Select(entry => new ProseItem(entry.Name, entry.Note, entry.Text))];
    }
}
