using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
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
        Assert.Equal([2, 3], sections.Select(section => section.Items.Count));

        var initials = window.GetVisualDescendants().OfType<InitialProse>().ToList();
        Assert.Equal(2, initials.Count);
        Assert.All(initials, initial => Assert.NotNull(initial.Icon));

        var tiles = window.GetVisualDescendants().OfType<StatTile>().ToList();
        Assert.Equal(6, tiles.Count);
        Assert.All(tiles, tile => Assert.NotEmpty(tile.GetVisualChildren()));

        window.Close();
    }

    [AvaloniaFact]
    public void A_click_on_a_section_heading_folds_its_content_and_the_next_one_unfolds_it()
    {
        var section = new ProseSectionView
        {
            Title = "Akcje",
            Intro = "Intro",
            Items = [new ProseItem("Cios", null, "Opis.")],
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };
        var window = Show(section);
        var heading = section.GetVisualDescendants().OfType<SectionHeading>().Single();
        var intro = section.FindControl<ProseText>("IntroText")!;
        Assert.True(intro.IsEffectivelyVisible);

        var point = heading.TranslatePoint(new Avalonia.Point(heading.Bounds.Width / 2, heading.Bounds.Height / 2), window)!.Value;
        var body = section.FindControl<SectionReveal>("Body")!;
        var openHeight = body.Bounds.Height;
        Assert.True(openHeight > 0);

        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(heading.IsExpanded);
        Assert.True(SectionRevealTests.Settle(body));
        Assert.False(intro.IsEffectivelyVisible);
        Assert.Equal(0, body.Bounds.Height);
        Assert.True(heading.IsEffectivelyVisible);

        window.MouseDown(point, Avalonia.Input.MouseButton.Left);
        window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.True(heading.IsExpanded);
        Assert.True(intro.IsEffectivelyVisible);
        Assert.True(SectionRevealTests.Settle(body));
        Assert.Equal(openHeight, body.Bounds.Height);

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
        var above = tiles.Single(tile => tile.Value == "19");
        Assert.Equal(BaseDeviation.Above, above.Deviation);
        Assert.Equal(ValueColor(above), ResourceColor("DungeonAboveBaseBrush"));
        Assert.Equal(ValueColor(tiles.Single(tile => tile.Value == "6 m")), ResourceColor("DungeonBelowBaseBrush"));
        Assert.Equal(ValueColor(tiles.Single(tile => tile.Value == "52")), ResourceColor("DungeonTextPrimaryBrush"));

        var tables = window.GetVisualDescendants().OfType<AbilityTableView>().ToList();
        Assert.Equal(2, tables.Count);
        var lowered = tables[0].GetVisualDescendants().OfType<SelectableTextBlock>().Single(t => t.Text == "12");
        Assert.Equal(ResourceColor("DungeonBelowBaseBrush"), ((ISolidColorBrush)lowered.Foreground!).Color);

        window.Close();
    }

    [AvaloniaFact]
    public void The_live_part_section_builds_every_tracked_value()
    {
        var window = Show(new LivePartSection());

        var tiles = window.GetVisualDescendants().OfType<TrackedValueTile>().ToList();
        Assert.Equal(5, tiles.Count);
        Assert.All(tiles, tile => Assert.NotEmpty(tile.GetVisualChildren()));
        Assert.Contains(tiles, tile => tile.Preview is not null);

        window.Close();
    }

    [AvaloniaFact]
    public void The_live_part_section_builds_the_note_samples()
    {
        var window = Show(new LivePartSection());

        // The tracked values' numbers are editable texts too, inside the tiles' templates.
        var notes = window.GetVisualDescendants().OfType<EditableText>().Where(text => text.TemplatedParent is null).ToList();
        Assert.Equal(5, notes.Count);
        Assert.All(notes, note => Assert.NotNull(note.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().SingleOrDefault()));

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
