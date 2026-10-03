using System.Linq;
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
/// Zakładka treści budowana w oknie: zasób złego typu w XAML-u (liczba tam, gdzie siatka chce
/// <see cref="GridLength"/>) kompiluje się i wywraca zakładkę dopiero przy utworzeniu widoku —
/// testy modelu widoku go nie widzą. Stawia widok z każdym rodzajem wiersza i szczegółu.
/// </summary>
public sealed class ContentTabViewBuildTests
{
    private static readonly ContentId TestSet = ContentId.Create("test");
    private static readonly ContentTypeReference SampleType = new(TestSet, ContentId.Create("sample"));

    private sealed record Sample(string Tier);

    private sealed class FakePresentation : IContentPresentation
    {
        public Control CreateCard(Entry entry, EntryPicture picture) => new HeaderLendingCard { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => Brushes.Gray;
    }

    /// <summary>A card that lends the detail header every piece, so the header's full layout builds.</summary>
    private sealed class HeaderLendingCard : TextBlock, IEntryCardHeader
    {
        public Control HeaderVisual { get; } = new ImageFrame { Width = 132, Height = 176 };

        public Control HeaderTitleEnd { get; } = new TextBlock { Text = "1,5 kg" };

        public Control HeaderTagsStart { get; } = new WordTag { Content = "Rzadki" };

        public Control HeaderBlock { get; } = new StatTile { Label = "Próbka", Value = "1", Note = "dopisek" };
    }

    private static ContentTabViewModel BuildViewModel(ContentRegistry registry)
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
            registry, new ContentTabDefinition("Próbki", [profile]), [SampleType], new FakePresentation());
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
}
