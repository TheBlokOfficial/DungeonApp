using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;
using DungeonApp.Desktop.Workspace.World;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// Builds the desk with the world catalog lying on it, in a window: a wrongly typed resource in the
/// catalog's view compiles but crashes when the view is created, which view-model tests cannot see.
/// Drives the catalog the way the GM does - clicks, Ctrl+N and the palette - on a system that names
/// two kinds of entity (one with a row hint) and one that has none.
/// </summary>
public sealed partial class WorldCatalogViewBuildTests
{
    private static readonly ContentId PackId = ContentId.Create("pack");

    private static EntryAddress Address(string entry) => new(PackId, ContentId.Create(entry));

    [AvaloniaFact]
    public async Task The_catalog_lists_folders_before_entities_with_numbers_hints_and_a_warning_for_a_missing_entry()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;

        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.CreateFolderAsync(cave, "Skarbiec");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 10");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        await catalog.AddEntitiesAsync(Address("gone"), 1, cave);
        fixture.Settle();

        Assert.Equal(
            ["Świat", "Jaskinia", "Skarbiec", "gone", "Goblin 2", "Goblin 10", "Lina"],
            catalog.Rows.Select(row => row.Name));
        Assert.Equal([0, 1, 2, 2, 1, 1, 1], catalog.Rows.Select(row => row.Depth));

        var goblin = Row(catalog, "Goblin 10");
        Assert.Equal("№ 1", goblin.NumberText);
        Assert.Equal("7/7", goblin.Hint);
        Assert.Equal("DungeonIconSkull", goblin.IconKey);
        Assert.Null(Row(catalog, "Lina").Hint);
        Assert.Equal("DungeonIconBackpack", Row(catalog, "Lina").IconKey);
        Assert.True(Row(catalog, "gone").IsUnresolved);

        // Every row got a container: the template builds.
        Assert.Equal(catalog.Rows.Count, fixture.RowBorders().Count());
    }

    [AvaloniaFact]
    public async Task Click_ctrl_click_and_shift_click_select_in_display_order()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, cave, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, cave, "Goblin 10");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        catalog.Click(Row(catalog, "Jaskinia"), ctrl: false, shift: false);
        catalog.Click(Row(catalog, "Goblin 10"), ctrl: false, shift: true);
        Assert.Equal(["Jaskinia", "Goblin 2", "Goblin 10"], catalog.SelectedRows.Select(row => row.Name));
        Assert.Equal(Row(catalog, "Goblin 10").EntityId, catalog.LastClickedEntity);

        catalog.Click(Row(catalog, "Lina"), ctrl: true, shift: false);
        Assert.Equal(["Jaskinia", "Goblin 2", "Goblin 10", "Lina"], catalog.SelectedRows.Select(row => row.Name));

        catalog.Click(Row(catalog, "Goblin 2"), ctrl: true, shift: false);
        Assert.Equal(["Jaskinia", "Goblin 10", "Lina"], catalog.SelectedRows.Select(row => row.Name));

        catalog.Click(Row(catalog, "Goblin 2"), ctrl: false, shift: false);
        Assert.Equal(["Goblin 2"], catalog.SelectedRows.Select(row => row.Name));
    }

    [AvaloniaFact]
    public async Task A_pointer_press_selects_the_row_under_it_and_ctrl_adds()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        fixture.Press(Row(catalog, "Goblin 2"), RawInputModifiers.None);
        Assert.Equal(["Goblin 2"], catalog.SelectedRows.Select(row => row.Name));

        fixture.Press(Row(catalog, "Lina"), RawInputModifiers.Control);
        Assert.Equal(["Goblin 2", "Lina"], catalog.SelectedRows.Select(row => row.Name));
    }

    [AvaloniaFact]
    public async Task A_double_click_on_an_entity_asks_to_open_it_and_on_a_folder_folds_it()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        var opened = new List<EntityId>();
        catalog.OpenEntityRequested += opened.Add;

        catalog.Activate(Row(catalog, "Lina"));
        catalog.Activate(Row(catalog, "Jaskinia"));

        Assert.Equal([Row(catalog, "Lina").EntityId!.Value], opened);
        Assert.True(Row(catalog, "Jaskinia").IsExpanded);
    }

    [AvaloniaFact]
    public async Task Ctrl_N_opens_the_palette_for_the_selected_folder_and_three_goblins_arrive_selected()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.CreateFolderAsync(cave, "Skarbiec");
        var vault = catalog.SelectedFolders.Single();
        catalog.SetExpanded(Row(catalog, "Jaskinia"), false);
        fixture.Settle();
        Assert.False(Row(catalog, "Jaskinia").IsExpanded);

        catalog.Click(Row(catalog, "Jaskinia"), ctrl: false, shift: false);
        fixture.CatalogView.Focus();
        fixture.Key(Key.Right);
        fixture.Key(Key.Down);
        Assert.Equal(["Skarbiec"], catalog.SelectedRows.Select(row => row.Name));

        fixture.Key(Key.N, RawInputModifiers.Control);
        var palette = fixture.Overlay.Children.OfType<Palette>().Single();
        Assert.Equal("Dodaj do: Skarbiec", palette.TargetText);
        Assert.Contains("4 gob", palette.Hint);

        fixture.Type("3 gob");
        Assert.Equal(["Goblin ×3"], palette.Rows.Select(row => row.DisplayName));
        fixture.Key(Key.Enter);
        fixture.WaitUntil(() => catalog.SelectedEntities.Count == 3);

        Assert.Equal(["Goblin", "Goblin", "Goblin"], catalog.SelectedRows.Select(row => row.Name));
        Assert.Equal([1, 2, 3], catalog.SelectedRows.Select(row => row.NumberText).Select(text => int.Parse(text![2..])));
        Assert.True(Row(catalog, "Jaskinia").IsExpanded);
        Assert.True(Row(catalog, "Skarbiec").IsExpanded);
        Assert.Equal(vault, Row(catalog, "Skarbiec").FolderId);
        Assert.Empty(fixture.Overlay.Children.OfType<Palette>());
    }

    [AvaloniaFact]
    public void The_palette_lists_only_entries_of_listed_types_with_tags_and_a_badge()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        var options = catalog.CreateAddOptions(catalog.ResolveAddTarget());

        var all = options.Search(string.Empty);

        Assert.Equal(["Goblin", "Lina", "Sowiedźmin"], all.Select(item => item.Name));
        var goblin = all.First(item => item.Name == "Goblin");
        Assert.Equal("DungeonIconSkull", goblin.IconKey);
        Assert.Equal("goblinoid, mały", goblin.Tags);
        Assert.Equal("1/4", goblin.Badge);
        Assert.NotNull(goblin.BadgeBrush);
        Assert.Equal("Dodaj do: Świat", options.TargetText);
    }

    [AvaloniaFact]
    public async Task Ctrl_Shift_N_makes_a_folder_next_to_the_selected_entity_and_selects_it()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("lina"), 1, cave);
        fixture.Settle();
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        fixture.CatalogView.Focus();

        fixture.Key(Key.N, RawInputModifiers.Control | RawInputModifiers.Shift);
        fixture.WaitUntil(() => catalog.Rows.Any(row => row.Name == "Nowy katalog"));

        var created = Row(catalog, "Nowy katalog");
        Assert.Equal([created], catalog.SelectedRows);
        Assert.Equal(2, created.Depth);
        Assert.Equal(cave, fixture.Entries.Snapshot.Get(WorldModels.Folders).Values.Single(folder => folder.Name == "Nowy katalog").ParentId);
    }

    [AvaloniaFact]
    public async Task Without_a_selection_the_target_is_the_root()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        catalog.Click(Row(catalog, "Jaskinia"), ctrl: true, shift: false);
        Assert.Empty(catalog.SelectedRows);

        Assert.Equal(new WorldAddTarget(null, "Świat"), catalog.ResolveAddTarget());
    }

    [AvaloniaFact]
    public async Task Place_width_and_expansion_come_back_from_the_saved_layout()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        catalog.SetExpanded(Row(catalog, "Jaskinia"), true);
        catalog.SetSurface(1000, 700, WorkspaceMetrics.Fallback);

        // The default place is the desk's top-right corner, on the grid.
        Assert.Equal(PanelGeometry.SnapToGrid(1000 - WorkspaceGridSettings.CatalogDefaultWidth - WorkspaceGridSettings.CatalogEdgeInset, 16), catalog.X);
        Assert.Equal(WorkspaceGridSettings.CatalogEdgeInset, catalog.Y);

        catalog.MoveTo(500.4, 100.9, commit: true);
        catalog.ResizeTo(350, commit: true);
        var layout = catalog.CreateLayout();

        Assert.Equal(496, layout.X);
        Assert.Equal(96, layout.Y);
        Assert.Equal(352, layout.Width);
        Assert.Single(layout.ExpandedFolders);

        var restored = new WorldCatalogViewModel(fixture.Entries, fixture.World, layout);
        restored.SetSurface(1000, 700, WorkspaceMetrics.Fallback);

        Assert.Equal(496, restored.X);
        Assert.Equal(96, restored.Y);
        Assert.Equal(352, restored.Width);
        Assert.True(Row(restored, "Jaskinia").IsExpanded);

        restored.SetExpanded(restored.Rows[0], false);
        Assert.Single(restored.Rows);
        Assert.True(restored.CreateLayout().IsRootCollapsed);
    }

    [AvaloniaFact]
    public void A_folder_that_no_longer_exists_is_forgotten_when_the_layout_is_saved()
    {
        using var fixture = new Fixture();
        var layout = new WorldCatalogLayout(null, null, 288, false, [Guid.NewGuid().ToString()]);
        var catalog = new WorldCatalogViewModel(fixture.Entries, fixture.World, layout);

        Assert.Empty(catalog.CreateLayout().ExpandedFolders);
    }

    private static WorldRowViewModel Row(WorldCatalogViewModel catalog, string name) =>
        catalog.Rows.Single(row => row.Name == name);

    private sealed record Sample(string Kind, string Size);

    /// <summary>
    /// A system with two kinds of entity (the first with a row hint) and one kind that has none,
    /// three entries and a pack that names none of them.
    /// </summary>
    private sealed class WorldTestSystem : IGameSystem, IContentTypeCatalog, IContentPresentation
    {
        private static readonly ContentId Set = ContentId.Create("test");
        private static readonly ContentTypeReference Being = new(Set, ContentId.Create("being"));
        private static readonly ContentTypeReference Thing = new(Set, ContentId.Create("thing"));
        private static readonly ContentTypeReference Note = new(Set, ContentId.Create("note"));

        private readonly ContentRegistry _registry;

        public WorldTestSystem()
        {
            var entries = new[]
            {
                Make("goblin", "Goblin", Being, new Sample("goblinoid", "mały")),
                Make("sowiedzmin", "Sowiedźmin", Being, new Sample("humanoid", "średni")),
                Make("lina", "Lina", Thing, new Sample("sprzęt", "mały")),
                Make("zasady", "Zasady", Note, new Sample("zasada", "mały")),
            };
            _registry = new ContentRegistry(
                [new Pack(PackId, "Paczka", new PackVersion(1, 0), [.. entries.Select(entry => entry.Entry)])],
                [.. entries],
                [],
                []);
        }

        public SystemId Id { get; } = SystemId.Create("world-test");

        public string DisplayName => "World test";

        public IReadOnlyList<SystemTabDeclaration> SystemTabs { get; } = [];

        public IReadOnlyList<CampaignTabDeclaration> CampaignTabs { get; } = [];

        public IReadOnlyList<StateModelDeclaration> StateModels { get; } = [];

        public IReadOnlyList<IStartupStep> StartupSteps { get; } = [];

        public IReadOnlyList<WorldEntityType> EntityTypes { get; } =
        [
            new WorldEntityType(Being, "DungeonIconSkull"),
            new WorldEntityType(Thing, "DungeonIconBackpack"),
        ];

        public IReadOnlyList<WorkspacePanelDescriptor> CreateDeskTools(CampaignTabContext context) => [];

        public WorldCatalogSource GetWorldCatalogSource() =>
            new(
                _registry,
                this,
                this,
                [
                    new ContentTypeProfile<Sample>(Being, tags: sample => [sample.Kind, sample.Size], badge: _ => new ContentBadge("1/4", "tier")),
                    new ContentTypeProfile<Sample>(Thing, tags: sample => [sample.Kind]),
                ]);

        public string? RowHint(ResolvedEntity entity) =>
            entity.Source?.Entry.Type == Being && entity.Unresolved is null ? "7/7" : null;

        public bool HasSet(ContentId set) => set == Set;

        public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
        {
            descriptor = new ContentTypeDescriptor(reference, reference.Type.ToString(), 1);
            return reference.Set == Set;
        }

        public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
        {
            error = null;
            return reference.Set == Set;
        }

        public Control CreateCard(Entry entry, EntryPicture picture) => new TextBlock { Text = entry.Name };

        public IBrush? ResolveBadgeBrush(string colorKey) => Brushes.Gray;

        private static RegisteredEntry Make(string id, string name, ContentTypeReference type, Sample sample)
        {
            var entry = new Entry(ContentId.Create(id), name, type, TypeVersion: 1, ContentValues.From(sample));

            return RegisteredEntry.CreateResolved(
                new EntryAddress(PackId, ContentId.Create(id)), entry, new ContentTypeDescriptor(type, type.Type.ToString(), 1));
        }
    }

    /// <summary>The desk over an in-memory campaign, in a window with the overlay the palette needs.</summary>
    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            World = new WorldTestSystem();
            var session = new CampaignSession(
                Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System),
                new InMemoryCampaignRepository(),
                WorldModels.Combine([]));
            var source = World.GetWorldCatalogSource();
            Entries = new CampaignEntriesContext(new CampaignTabContext(session), source.Registry, source.Types);
            var store = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-world-tests-{Guid.NewGuid():N}"));
            Desk = new CampaignWorkspaceViewModel(
                store, Guid.NewGuid().ToString(), WorkspaceLayout.Empty, [], () => Task.CompletedTask, Entries, World);

            Overlay = new WindowOverlay();
            View = new CampaignWorkspaceView { DataContext = Desk };
            Window = new Window { Width = 1200, Height = 800, Content = new Panel { Children = { View, Overlay } } };
            Window.Show();
            Settle();
        }

        public WorldTestSystem World { get; }

        public CampaignEntriesContext Entries { get; }

        public CampaignWorkspaceViewModel Desk { get; }

        public WorldCatalogViewModel Catalog => Desk.Catalog;

        public WindowOverlay Overlay { get; }

        public CampaignWorkspaceView View { get; }

        public Window Window { get; }

        public WorldCatalogView CatalogView => View.GetVisualDescendants().OfType<WorldCatalogView>().Single();

        public IEnumerable<Border> RowBorders() =>
            CatalogView.GetVisualDescendants().OfType<Border>().Where(border => border.Classes.Contains("world-row"));

        public void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();
        }

        public void WaitUntil(Func<bool> condition)
        {
            for (var attempt = 0; attempt < 50 && !condition(); attempt++)
            {
                Settle();
                Thread.Sleep(10);
            }

            Assert.True(condition());
        }

        public void Key(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            Window.KeyPress(key, modifiers, PhysicalKey.None, null);
            Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
            Settle();
        }

        public void Type(string text)
        {
            Window.KeyTextInput(text);
            Settle();
        }

        public void Press(WorldRowViewModel row, RawInputModifiers modifiers)
        {
            var border = RowBorders().Single(candidate => candidate.DataContext == row);
            var point = border.TranslatePoint(new Point(60, border.Bounds.Height / 2), Window)!.Value;
            Window.MouseDown(point, MouseButton.Left, modifiers);
            Window.MouseUp(point, MouseButton.Left, modifiers);
            Settle();
        }

        public void Dispose()
        {
            Window.Close();
            Desk.Dispose();
        }
    }
}
