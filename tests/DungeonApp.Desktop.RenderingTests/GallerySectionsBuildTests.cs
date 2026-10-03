using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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

        var tiles = window.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(6, tiles.Count);
        Assert.All(tiles, tile => Assert.NotEmpty(tile.GetVisualChildren()));

        window.Close();
    }

    [AvaloniaFact]
    public void The_tables_and_compositions_sections_build_the_ability_tables()
    {
        var window = Show(new StackPanel { Children = { new TablesSection(), new CompositionsSection() } });

        var tables = window.GetVisualDescendants().OfType<AbilityTableView>().ToList();
        Assert.Equal(4, tables.Count);
        Assert.All(tables, table => Assert.Equal(3, table.Rows.Count));

        window.Close();
    }
}
