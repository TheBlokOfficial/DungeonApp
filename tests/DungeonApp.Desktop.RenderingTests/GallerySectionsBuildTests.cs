using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries.Controls;
using DungeonApp.Desktop.Shell.Gallery.Sections;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The gallery built in a window: a sample with a mistyped resource or a broken template compiles
/// and only fails when the gallery opens. Builds the sections that show a card's controls - no
/// assertions about looks, only that every sample got its template and its data.
/// </summary>
public sealed class GallerySectionsBuildTests
{
    private static Window Show(Control content)
    {
        var window = new Window { Width = 1280, Height = 1600, Content = new ScrollViewer { Content = content } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void The_card_parts_section_builds_every_sample()
    {
        var window = Show(new CardPartsSection());

        var headings = window.GetVisualDescendants().OfType<SectionHeading>().ToList();
        Assert.Equal(4, headings.Count);
        Assert.All(headings, heading => Assert.NotEmpty(heading.GetVisualChildren()));

        var sections = window.GetVisualDescendants().OfType<ProseSectionView>().ToList();
        Assert.Equal(2, sections.Count);
        Assert.All(sections, section => Assert.Equal(2, section.Items.Count));

        var initials = window.GetVisualDescendants().OfType<InitialProse>().ToList();
        Assert.Equal(2, initials.Count);
        Assert.All(initials, initial => Assert.NotNull(initial.Icon));

        var tiles = window.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(6, tiles.Count);
        Assert.All(tiles, tile => Assert.NotEmpty(tile.GetVisualChildren()));

        window.Close();
    }

    [AvaloniaFact]
    public void The_live_part_section_builds()
    {
        var window = Show(new LivePartSection());

        Assert.NotEmpty(window.GetVisualDescendants().OfType<TextBlock>());

        window.Close();
    }

    [AvaloniaFact]
    public void The_live_part_section_builds_gauges_and_values_against_their_base()
    {
        var window = Show(new LivePartSection());

        Assert.Equal(6, window.GetVisualDescendants().OfType<TrackGauge>().Count());

        var tiles = window.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(BaseDeviation.Above, tiles[0].Deviation);
        Assert.Equal(ValueColor(tiles[0]), ResourceColor("DungeonAboveBaseBrush"));
        Assert.Equal(ValueColor(tiles[1]), ResourceColor("DungeonBelowBaseBrush"));
        Assert.Equal(ValueColor(tiles[2]), ResourceColor("DungeonTextPrimaryBrush"));

        var tables = window.GetVisualDescendants().OfType<AbilityTableView>().ToList();
        Assert.Equal(2, tables.Count);
        var lowered = tables[0].GetVisualDescendants().OfType<SelectableTextBlock>().Single(t => t.Text == "12");
        Assert.Equal(ResourceColor("DungeonBelowBaseBrush"), ((ISolidColorBrush)lowered.Foreground!).Color);

        window.Close();
    }

    [AvaloniaFact]
    public void The_live_part_marker_rows_keep_one_height_with_and_without_a_detail_or_removal()
    {
        var section = new LivePartSection();
        var window = Show(section);

        var rows = section.GetVisualDescendants().OfType<FramedIconRow>().ToList();
        Assert.Equal(6, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.NotEmpty(row.GetVisualChildren());
            Assert.NotNull(row.Icon);
        });

        var plain = section.FindControl<FramedIconRow>("ConditionRow")!;
        var bare = section.FindControl<FramedIconRow>("ConditionBareRow")!;
        var removed = section.FindControl<FramedIconRow>("ConditionRemovedRow")!;
        Assert.Equal(plain.Bounds.Height, bare.Bounds.Height);
        Assert.Equal(plain.Bounds.Height, removed.Bounds.Height);

        window.Close();
    }

    private static Avalonia.Media.Color ValueColor(StatTile tile) =>
        ((ISolidColorBrush)tile.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Single(t => t.Name == "PART_Value").Foreground!).Color;

    private static Avalonia.Media.Color ResourceColor(string key) =>
        ((ISolidColorBrush)Avalonia.Application.Current!.FindResource(key)!).Color;

    [AvaloniaFact]
    public void The_tables_and_compositions_sections_build_the_ability_tables()
    {
        var window = Show(new StackPanel { Children = { new TablesSection(), new CompositionsSection() } });

        var tables = window.GetVisualDescendants().OfType<AbilityTableView>().ToList();
        Assert.Equal(4, tables.Count);
        Assert.All(tables, table => Assert.Equal(3, table.Rows.Count));

        // Each ability is one column: its label above the grid, its score and modifier as two cells.
        Assert.All(tables, table =>
        {
            var descendants = table.GetVisualDescendants().ToList();
            Assert.Equal(6, descendants.OfType<TableCell>().Count());
            Assert.Equal(3, descendants.OfType<SelectableTextBlock>().Count(block => block.Classes.Contains("ability-label")));
        });

        window.Close();
    }
}
