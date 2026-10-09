using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Desktop.Workspace.Panels;
using DungeonApp.Desktop.Workspace.World;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// The preview and the pinned windows built in a window over a desk with a world: the card of an
/// entity (a creature's and an item's, drawn by the real D&amp;D cards) under the entity's path with
/// its № line, the preview following the last clicked entity, the pin, a removal and an entity whose
/// entry is gone.
/// <para>
/// Scenario for the render tool: <c>desk.Catalog.AddEntitiesAsync(address, 1, folder, label)</c>, then
/// <c>desk.Catalog.Click(row, ctrl: false, shift: false)</c>, <c>desk.OpenPreview()</c> and
/// <c>desk.PinEntity(id)</c>; the windows lie on the desk as any other.
/// </para>
/// </summary>
public sealed class EntityPreviewBuildTests
{
    private static readonly ContentId PackId = ContentId.Create("pack");

    private static EntryAddress Address(string entry) => new(PackId, ContentId.Create(entry));

    [AvaloniaFact]
    public async Task The_preview_opens_on_a_double_click_empty_and_follows_the_last_clicked_entity()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        Assert.Empty(fixture.Desk.Panels);

        catalog.Activate(Row(catalog, "Szef"));
        fixture.Settle();
        var preview = Assert.Single(fixture.Desk.Panels);
        var card = Assert.IsType<EntityCardViewModel>(preview.Body);
        Assert.Equal("Podgląd", preview.Title);
        var detail = Assert.IsType<ValidContentDetailViewModel>(card.Detail);
        Assert.Equal(["Świat", "Szef"], detail.Breadcrumbs);
        Assert.Equal("Szef", detail.Name);
        Assert.Equal("№ 1 · Goblin", detail.Caption);
        Assert.IsType<CreatureCardView>(detail.Card);
        Assert.NotNull(FindView(fixture, preview));

        // A click moves the preview to the other entity; a second double click only raises it.
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        var item = Assert.IsType<ValidContentDetailViewModel>(card.Detail);
        Assert.Equal("Lina", item.Name);
        Assert.Equal("№ 2 · Lina", item.Caption);
        Assert.IsType<GearCardView>(item.Card);

        catalog.Activate(Row(catalog, "Lina"));
        Assert.Single(fixture.Desk.Panels);
    }

    [AvaloniaFact]
    public void An_empty_preview_says_what_to_click()
    {
        using var fixture = new Fixture();

        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        var card = Assert.IsType<EntityCardViewModel>(preview.Body);
        Assert.True(card.IsEmpty);
        var text = FindView(fixture, preview)!.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == EntityCardViewModel.EmptyText);
        Assert.True(text.IsEffectivelyVisible);
        Assert.False(preview.HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPin").Command.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task The_pin_sets_the_card_aside_and_the_preview_keeps_following_the_catalog()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        var pin = preview.HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPin");
        pin.Command.Execute(null);
        fixture.Settle();

        Assert.Equal(2, fixture.Desk.Panels.Count);
        var pinned = fixture.Desk.Panels.Single(panel => panel.Descriptor.Id == CampaignWorkspaceViewModel.PinnedDescriptorId);
        Assert.Equal("Szef · № 1", pinned.Title);
        Assert.Equal(Row(catalog, "Szef").EntityId!.Value.Value.ToString(), pinned.InstanceKey);
        Assert.NotNull(FindView(fixture, pinned));

        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        Assert.Equal("Lina", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)preview.Body).Detail).Name);
        Assert.Equal("Szef", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)pinned.Body).Detail).Name);

        // Pinning the same entity again brings its window up instead of making another.
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        pin.Command.Execute(null);
        Assert.Equal(2, fixture.Desk.Panels.Count);
        Assert.True(pinned.IsActive);

        // A second entity gets its own window, and the cross removes one for good.
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        pin.Command.Execute(null);
        Assert.Equal(3, fixture.Desk.Panels.Count);
        pinned.CloseCommand.Execute(null);
        Assert.Equal(2, fixture.Desk.Panels.Count);
        Assert.Empty(fixture.Desk.MinimizedPanels);
    }

    [AvaloniaFact]
    public async Task Removing_an_entity_closes_its_pinned_window_and_empties_the_preview_that_showed_it()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        fixture.Settle();
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        var id = Row(catalog, "Szef").EntityId!.Value;
        var pinned = fixture.Desk.PinEntity(id)!;
        fixture.Settle();

        await fixture.Entries.ChangeAsync(WorldChanges.DeleteEntities([id]));
        fixture.Settle();

        Assert.Equal([preview], fixture.Desk.Panels);
        Assert.True(((EntityCardViewModel)preview.Body).IsEmpty);
        Assert.DoesNotContain(pinned, fixture.Desk.Panels);
    }

    [AvaloniaFact]
    public async Task Renaming_a_folder_refreshes_the_path_on_the_card_and_the_pinned_title_follows_the_name()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.CreateFolderAsync(null, "Kopalnia");
        var folder = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, folder, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        var pinned = fixture.Desk.PinEntity(id)!;
        var card = (EntityCardViewModel)pinned.Body;
        Assert.Equal(["Świat", "Kopalnia", "Szef"], Assert.IsType<ValidContentDetailViewModel>(card.Detail).Breadcrumbs);

        await fixture.Entries.ChangeAsync(WorldChanges.RenameFolder(fixture.Entries.Snapshot, folder, "Jaskinia"));
        fixture.Settle();

        Assert.Equal(["Świat", "Jaskinia", "Szef"], Assert.IsType<ValidContentDetailViewModel>(card.Detail).Breadcrumbs);
    }

    [AvaloniaFact]
    public async Task An_entity_whose_entry_is_gone_shows_the_reason_with_its_number_and_name()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("gone"), 1, null, "Duch");
        fixture.Settle();
        catalog.Click(Row(catalog, "Duch"), ctrl: false, shift: false);

        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        var broken = Assert.IsType<BrokenContentDetailViewModel>(((EntityCardViewModel)preview.Body).Detail);
        Assert.Equal("№ 1 · bez wpisu", broken.Overline);
        Assert.Equal("Duch", broken.Name);
        Assert.Contains("nie zawiera już wpisu", broken.Reason);
        Assert.NotNull(FindView(fixture, preview));
    }

    private static WorldRowViewModel Row(WorldCatalogViewModel catalog, string name) =>
        catalog.Rows.Single(row => row.Name == name);

    private static EntityCardView? FindView(Fixture fixture, WorkspacePanelViewModel panel) =>
        fixture.View.GetVisualDescendants().OfType<EntityCardView>().SingleOrDefault(view => view.DataContext == panel.Body);

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            var system = new PreviewTestSystem();
            var session = new CampaignSession(
                Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System),
                new InMemoryCampaignRepository(),
                WorldModels.Combine([]));
            var source = system.GetWorldCatalogSource();
            Entries = new CampaignEntriesContext(new CampaignTabContext(session), source.Registry, source.Types);
            var store = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-preview-tests-{Guid.NewGuid():N}"));
            Desk = new CampaignWorkspaceViewModel(
                store, Guid.NewGuid().ToString(), WorkspaceLayout.Empty, [], () => Task.CompletedTask, Entries, system);

            View = new CampaignWorkspaceView { DataContext = Desk };
            Window = new Window { Width = 1400, Height = 900, Content = View };
            Window.Show();
            Settle();
        }

        public CampaignEntriesContext Entries { get; }

        public CampaignWorkspaceViewModel Desk { get; }

        public CampaignWorkspaceView View { get; }

        public Window Window { get; }

        public void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Window.Close();
            Desk.Dispose();
        }
    }

    private sealed record Sample(string Kind);

    /// <summary>A creature, an item and the real D&amp;D cards for them; the card does not read the entry's values.</summary>
    private sealed class PreviewTestSystem : IGameSystem, IContentTypeCatalog, IContentPresentation
    {
        private static readonly ContentId Set = ContentId.Create("test");
        private static readonly ContentTypeReference Being = new(Set, ContentId.Create("being"));
        private static readonly ContentTypeReference Thing = new(Set, ContentId.Create("thing"));

        private static readonly Creature Creature = new()
        {
            Size = "Mały",
            Type = "humanoid",
            Alignment = "neutralny zły",
            Combat = new CombatAspect { Ac = 15, Hp = 7, Str = 8, Dex = 14, Con = 10, Int = 10, Wis = 8, Cha = 8 },
            Speed = "9 m",
            Senses = "pasywna Percepcja 9",
            Challenge = "1/4",
            Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Maczuga", Text = "+4 do trafienia." }] },
        };

        private static readonly Gear Gear = new()
        {
            Rarity = "Pospolity",
            Category = "Ekwipunek",
            Item = new ItemAspect { Weight = 5, Value = 1 },
        };

        private readonly ContentRegistry _registry;

        public PreviewTestSystem()
        {
            var entries = new[]
            {
                Make("goblin", "Goblin", Being),
                Make("lina", "Lina", Thing),
            };
            _registry = new ContentRegistry(
                [new Pack(PackId, "Paczka", new PackVersion(1, 0), [.. entries.Select(entry => entry.Entry)])],
                [.. entries],
                [],
                []);
        }

        public SystemId Id { get; } = SystemId.Create("preview-test");

        public string DisplayName => "Preview test";

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
                    new ContentTypeProfile<Sample>(Being, tags: sample => [sample.Kind]),
                    new ContentTypeProfile<Sample>(Thing, tags: sample => [sample.Kind]),
                ]);

        public string? RowHint(ResolvedEntity entity) => null;

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

        public Control CreateCard(Entry entry, EntryPicture picture)
        {
            if (entry.Type == Being)
            {
                var creature = new CreatureCardView();
                creature.SetCreature(Creature, picture);
                return creature;
            }

            var gear = new GearCardView();
            gear.SetGear(Gear, picture);
            return gear;
        }

        public IBrush? ResolveBadgeBrush(string colorKey) => Brushes.Gray;

        private static RegisteredEntry Make(string id, string name, ContentTypeReference type)
        {
            var entry = new Entry(ContentId.Create(id), name, type, TypeVersion: 1, ContentValues.From(new Sample("rodzaj")));

            return RegisteredEntry.CreateResolved(
                new EntryAddress(PackId, ContentId.Create(id)), entry, new ContentTypeDescriptor(type, type.Type.ToString(), 1));
        }
    }
}
