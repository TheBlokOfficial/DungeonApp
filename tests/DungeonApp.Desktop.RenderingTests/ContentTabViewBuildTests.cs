using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Builds the content tab in a window: a wrongly typed XAML resource (a number where the grid expects
/// <see cref="GridLength"/>) compiles but crashes the tab when the view is created;
/// view-model tests cannot catch it. Builds the view with every kind of row and detail.
/// </summary>
public sealed class ContentTabViewBuildTests
{
    private static readonly ContentId TestSet = ContentId.Create("test");
    private static readonly ContentTypeReference SampleType = new(TestSet, ContentId.Create("sample"));

    private sealed record Sample(string Tier);

    private sealed class FakePresentation(bool blockFollowsTitle = false) : IContentPresentation
    {
        public Control CreateCard(Entry entry, EntryPicture picture) =>
            new HeaderLendingCard { Text = entry.Name, HeaderBlockFollowsTitle = blockFollowsTitle };

        public IBrush? ResolveBadgeBrush(string colorKey) => Brushes.Gray;
    }

    /// <summary>A card that lends the detail header every piece, so the header's full layout builds.</summary>
    private sealed class HeaderLendingCard : TextBlock, IEntryCardHeader
    {
        public Control HeaderVisual { get; } = new ImageFrame { Width = 150, Height = 200 };

        public Control HeaderTitleEnd { get; } = new TextBlock { Text = "1,5 kg" };

        public Control HeaderTagsStart { get; } = new WordTag { Content = "Rzadki" };

        public Control HeaderBlock { get; } = new StatTile { Label = "Próbka", Value = "1", Note = "dopisek" };

        public bool HeaderBlockFollowsTitle { get; init; }
    }

    private static ContentTabViewModel BuildViewModel(ContentRegistry registry, bool blockFollowsTitle = false)
    {
        var profile = new ContentTypeProfile<Sample>(
            SampleType,
            category: new ContentCategorySpec<Sample>("Grupa", sample => sample.Tier),
            tags: sample => [sample.Tier],
            badge: sample => new ContentBadge(sample.Tier, "tier-key"),
            valueFilters:
            [
                new ContentValueFilterSpec<Sample>("Poziom", sample => sample.Tier, System.StringComparer.Ordinal),
                new ContentValueFilterSpec<Sample>("Pusty", _ => null, System.StringComparer.Ordinal),
            ],
            sorts: [new ContentSortSpec<Sample>("Poziom", (a, b) => string.CompareOrdinal(a.Tier, b.Tier))]);
        return new ContentTabViewModel(
            registry, new ContentTabDefinition("Próbki", [profile]), [SampleType], new FakePresentation(blockFollowsTitle));
    }

    private static Entry MakeEntry(string id, string name) =>
        new(ContentId.Create(id), name, SampleType, TypeVersion: 1, ContentValues.From(new Sample("1")));

    private static ContentRegistry FullRegistry()
    {
        var entry = MakeEntry("a", "Alfa");
        return new ContentRegistry(
            [new Pack(ContentId.Create("p"), "Paczka", new PackVersion(1, 0), [entry])],
            [
                RegisteredEntry.CreateResolved(
                    new EntryAddress(ContentId.Create("p"), ContentId.Create("a")),
                    entry,
                    new ContentTypeDescriptor(SampleType, "Sample", 1)),
                RegisteredEntry.CreateUnresolved(
                    new EntryAddress(ContentId.Create("p"), ContentId.Create("z")),
                    MakeEntry("z", "Duch"),
                    EntryUnresolvedReason.MissingSet,
                    null),
            ],
            [],
            [new RejectedPack("zla-paczka", "manifest is unreadable.")]);
    }

    private static Window Show(ContentTabViewModel viewModel)
    {
        var window = new Window
        {
            Width = 1280,
            Height = 800,
            Content = new ContentTabView { DataContext = viewModel },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void The_tab_builds_and_shows_every_kind_of_detail()
    {
        var viewModel = BuildViewModel(FullRegistry());
        var window = Show(viewModel);

        foreach (var row in viewModel.Sections.SelectMany(section => section.Rows).ToList())
        {
            row.SelectCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(viewModel.Detail);

            if (viewModel.Detail is ValidContentDetailViewModel detail)
            {
                Assert.True(detail.HeaderVisual!.IsEffectivelyVisible);
                Assert.NotEmpty(detail.HeaderBlock!.GetVisualChildren());
            }
        }

        Assert.Equal(3, viewModel.Sections.SelectMany(section => section.Rows).Count());
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_header_block_sits_at_the_visual_foot_or_under_the_title(bool followsTitle)
    {
        var viewModel = BuildViewModel(FullRegistry(), followsTitle);
        var window = Show(viewModel);
        viewModel.Sections.SelectMany(section => section.Rows).First().SelectCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var detail = Assert.IsType<ValidContentDetailViewModel>(viewModel.Detail);
        var visual = detail.HeaderVisual!;
        var block = detail.HeaderBlock!;
        var visualBottom = visual.TranslatePoint(new Point(0, visual.Bounds.Height), window)!.Value.Y;
        var blockBottom = block.TranslatePoint(new Point(0, block.Bounds.Height), window)!.Value.Y;

        if (followsTitle)
        {
            Assert.True(blockBottom < visualBottom - 40, $"block ends at {blockBottom}, visual at {visualBottom}");
        }
        else
        {
            Assert.Equal(visualBottom, blockBottom, 1.0);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void The_filter_rows_build_through_a_chosen_value_and_wyczysc_filtry()
    {
        var viewModel = BuildViewModel(FullRegistry());
        var window = Show(viewModel);

        var labels = window.GetVisualDescendants().OfType<TextBlock>()
            .Where(text => text.Classes.Contains("filter-label"))
            .ToList();
        Assert.Equal(["Grupa", "Poziom", "Pusty", "Paczka"], labels.Select(label => label.Text));
        Assert.Equal([true, true, false, true], labels.Select(label => label.IsEnabled));

        var pickers = window.GetVisualDescendants().OfType<DropDownPicker>().ToList();
        Assert.Equal(["Wszystkie", "Wszystkie", "Wszystkie", "Wszystkie"], pickers.Select(picker => picker.PlaceholderText));
        Assert.Equal([true, true, false, true], pickers.Select(picker => picker.IsEnabled));

        var levelChip = viewModel.Filters.Single(filter => filter.Label == "Poziom");
        levelChip.SelectedValues.Add("1");
        viewModel.Search = "Al";
        Dispatcher.UIThread.RunJobs();

        Assert.True(levelChip.IsActive);
        Assert.True(viewModel.ClearFiltersCommand.CanExecute(null));

        viewModel.ClearFiltersCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(levelChip.SelectedValues);
        Assert.Equal(string.Empty, viewModel.Search);
        Assert.False(viewModel.ClearFiltersCommand.CanExecute(null));
        window.Close();
    }

    [AvaloniaFact]
    public void The_sort_picker_builds_through_both_directions_of_both_kinds_of_field()
    {
        var viewModel = BuildViewModel(FullRegistry());
        var window = Show(viewModel);

        var picker = window.GetVisualDescendants().OfType<SortPicker>().Single();
        Assert.True(picker.CanReverse);
        Assert.Equal(["Poziom"], picker.NumericOptions);

        picker.IsDescending = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(viewModel.SortDescending);

        picker.SelectedOption = "Poziom";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Poziom", viewModel.SelectedSort);

        picker.IsDescending = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(viewModel.SortDescending);

        viewModel.Search = "Al";
        viewModel.ClearFiltersCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Poziom", viewModel.SelectedSort);
        window.Close();
    }

    [AvaloniaFact]
    public void The_tab_builds_with_no_entries()
    {
        var viewModel = BuildViewModel(new ContentRegistry([], [], [], []));
        var window = Show(viewModel);

        Assert.True(viewModel.HasNoEntries);
        window.Close();
    }

    /// <summary>The visible icons standing before the rows' names (not the tab's other icons).</summary>
    private static System.Collections.Generic.List<ForegroundIcon> RowIcons(Window window) =>
        [.. window.GetVisualDescendants().OfType<ForegroundIcon>()
            .Where(icon => icon.IsEffectivelyVisible && icon.Parent is Grid grid && grid.Classes.Contains("row-picture"))];

    [AvaloniaFact]
    public void A_tab_whose_rows_show_no_picture_keeps_no_place_for_one()
    {
        var viewModel = BuildViewModel(FullRegistry());
        var window = Show(viewModel);

        Assert.All(viewModel.Sections.SelectMany(section => section.Rows), row => Assert.False(row.HasPictureSlot));
        Assert.Empty(RowIcons(window));
        window.Close();
    }

    private sealed class PicturedCatalog : IContentTypeCatalog
    {
        public bool HasSet(ContentId set) => set == TestSet;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = new ContentTypeDescriptor(SampleType, "Sample", 1, "picture");
            return reference == SampleType;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = null;
            return true;
        }
    }

    /// <summary>
    /// A type that shows its pictures in rows: every row keeps the place before its name - the entry
    /// with an icon shows it there, the one without leaves it empty, so the names stand in one column.
    /// The icon is read from the pack with the real decoder.
    /// </summary>
    [AvaloniaFact]
    public async System.Threading.Tasks.Task A_tab_whose_rows_show_pictures_puts_each_icon_before_its_name()
    {
        var packs = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-row-pictures-{System.Guid.NewGuid():N}");
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packs, "p", "entries"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packs, "p", "ikony"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(packs, "p", "pack.json"),
                """{ "formatVersion": 1, "id": "p", "name": "Paczka", "version": { "major": 1, "minor": 0 } }""");
            System.IO.File.WriteAllText(System.IO.Path.Combine(packs, "p", "entries", "a.json"),
                """{ "id": "a", "name": "Alfa", "template": "test:sample", "templateVersion": 1, "values": { "picture": "ikony/a.svg" } }""");
            System.IO.File.WriteAllText(System.IO.Path.Combine(packs, "p", "entries", "b.json"),
                """{ "id": "b", "name": "Beta", "template": "test:sample", "templateVersion": 1, "values": {} }""");
            System.IO.File.WriteAllText(System.IO.Path.Combine(packs, "p", "ikony", "a.svg"),
                """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path d="M0 0h512v512H0z"/></svg>""");

            var registry = await new ContentPackLoader(packs, new PicturedCatalog()).LoadAsync();
            var profile = new ContentTypeProfile<object>(SampleType, showsPictureInRow: true);
            var viewModel = new ContentTabViewModel(
                registry, new ContentTabDefinition("Próbki", [profile]), [SampleType], new FakePresentation());
            var window = Show(viewModel);

            var rows = viewModel.Sections.SelectMany(section => section.Rows).ToList();
            Assert.Equal(["Alfa", "Beta"], rows.Select(row => row.Name));
            Assert.All(rows, row => Assert.True(row.HasPictureSlot));
            Assert.NotNull(rows[0].Picture);
            Assert.Null(rows[1].Picture);

            var icons = RowIcons(window);
            Assert.Equal(2, icons.Count);
            Assert.Single(icons, icon => icon.Source is not null);
            Assert.Single(icons.Select(icon => icon.Bounds.X).Distinct());

            window.Close();
        }
        finally
        {
            System.IO.Directory.Delete(packs, recursive: true);
        }
    }
}
