using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Desktop.Content;
using DungeonApp.Library.Entries.Desktop.Features.ContentTab;

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
        public Control CreateCard(Entry entry) => new TextBlock { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => Brushes.Gray;
    }

    private static ContentTabViewModel BuildViewModel(ContentRegistry registry)
    {
        var profile = new ContentTypeProfile<Sample>(
            SampleType,
            tags: sample => [sample.Tier],
            badge: sample => new ContentBadge(sample.Tier, "tier-key"),
            valueFilters: [],
            sorts: []);
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
        }

        Assert.Equal(3, viewModel.Sections.SelectMany(section => section.Rows).Count());
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
