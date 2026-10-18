using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Entries.Controls;
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
        Assert.Equal(["Próbki", "rodzaj", "Goblin"], detail.Breadcrumbs);
        Assert.Equal("Szef", detail.Name);
        Assert.Equal("№ 1", detail.NumberLabel);
        Assert.IsType<CreatureCardView>(detail.Card);
        Assert.NotNull(FindView(fixture, preview));

        // A click moves the preview to the other entity; a second double click only raises it.
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        var item = Assert.IsType<ValidContentDetailViewModel>(card.Detail);
        Assert.Equal("Lina", item.Name);
        Assert.Equal("№ 2", item.NumberLabel);
        Assert.Equal(["Próbki", "Lina"], item.Breadcrumbs);
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

        var placement = preview.Desired;
        var body = preview.Body;
        var container = Container(fixture, preview);
        preview.HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPin").Command.Execute(null);
        fixture.Settle();

        // The same window turns into the pinned card: the same object and control (no ghost, no
        // appearing), same place and body, no second window, the unpin button.
        var pinned = Assert.Single(fixture.Desk.Panels);
        Assert.Same(preview, pinned);
        Assert.Same(body, pinned.Body);
        Assert.Same(container, Container(fixture, pinned));
        Assert.Empty(Ghosts(fixture).Children);
        Assert.Equal(CampaignWorkspaceViewModel.PinnedDescriptorId, pinned.Descriptor.Id);
        Assert.Equal(placement, pinned.Desired);
        Assert.Equal("Podgląd", pinned.Title);
        Assert.Equal(Row(catalog, "Szef").EntityId!.Value.Value.ToString(), pinned.InstanceKey);
        var unpin = pinned.HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPinned");
        Assert.Equal("Odepnij kartę", unpin.ToolTip);
        Assert.NotNull(FindView(fixture, pinned));

        // The pinned card stays on its entity; the next plain click makes a new preview, a step aside.
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        fixture.Settle();
        Assert.Equal("Szef", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)pinned.Body).Detail).Name);
        var second = fixture.Desk.Panels.Single(panel => panel.Descriptor.Id == CampaignWorkspaceViewModel.PreviewDescriptorId);
        Assert.Equal("Lina", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)second.Body).Detail).Name);
        Assert.Equal(placement.X + WorkspaceGridSettings.DefaultSnapStep, second.Desired.X);
        Assert.Equal(placement.Y + WorkspaceGridSettings.DefaultSnapStep, second.Desired.Y);

        // Unpinning turns the card back into the preview and closes the one there was.
        unpin.Command.Execute(null);
        fixture.Settle();
        var back = Assert.Single(fixture.Desk.Panels);
        Assert.Same(pinned, back);
        Assert.Same(container, Container(fixture, back));
        Assert.Equal(CampaignWorkspaceViewModel.PreviewDescriptorId, back.Descriptor.Id);
        Assert.Equal("Podgląd", back.Title);
        Assert.Equal(CampaignWorkspaceViewModel.PreviewDescriptorId, back.InstanceKey);
        Assert.Equal(placement, back.Desired);
        Assert.Equal("Lina", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)back.Body).Detail).Name);
        Assert.Empty(fixture.Desk.MinimizedPanels);

        // Pinned and unpinned again, the preview shows the current selection.
        back.HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPin").Command.Execute(null);
        fixture.Desk.Panels.Single().HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPinned").Command.Execute(null);
        Assert.Equal("Lina", Assert.IsType<ValidContentDetailViewModel>(((EntityCardViewModel)fixture.Desk.Panels.Single().Body).Detail).Name);
    }

    [AvaloniaFact]
    public async Task The_role_of_a_window_survives_a_saved_layout()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        fixture.Desk.OpenPreview().HeaderActions.Single(action => action.IconResourceKey == "DungeonIconPin").Command.Execute(null);
        catalog.Activate(Row(catalog, "Lina"));
        fixture.Desk.FlushLayout();

        using var restored = new CampaignWorkspaceViewModel(
            fixture.Store, fixture.WorkspaceId, await fixture.Store.LoadAsync(fixture.WorkspaceId), [], () => Task.CompletedTask,
            fixture.Entries, fixture.GameSystem);

        var ids = restored.Panels.Select(panel => panel.Descriptor.Id).Order().ToList();
        Assert.Equal([CampaignWorkspaceViewModel.PinnedDescriptorId, CampaignWorkspaceViewModel.PreviewDescriptorId], ids);
        var pinned = restored.Panels.Single(panel => panel.Descriptor.Id == CampaignWorkspaceViewModel.PinnedDescriptorId);
        Assert.Equal("Podgląd", pinned.Title);
        Assert.Single(pinned.HeaderActions, action => action.IconResourceKey == "DungeonIconPinned");
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
    public async Task The_path_names_the_entry_not_the_folders_and_the_titles_stay_when_the_entity_is_renamed()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.CreateFolderAsync(null, "Kopalnia");
        var folder = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, folder, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        var pinned = fixture.Desk.PinEntity(id)!;
        var preview = fixture.Desk.OpenPreview();
        var card = (EntityCardViewModel)pinned.Body;
        Assert.Equal(["Próbki", "rodzaj", "Goblin"], Assert.IsType<ValidContentDetailViewModel>(card.Detail).Breadcrumbs);
        Assert.Equal("Podgląd", pinned.Title);
        Assert.Equal("Podgląd", preview.Title);

        await fixture.Entries.ChangeAsync(WorldChanges.RenameFolder(fixture.Entries.Snapshot, folder, "Jaskinia"));
        await fixture.Entries.ChangeAsync(CampaignEntityChanges.Relabel(catalog.Tree.Entity(id)!.Entity, "Wódz"));
        fixture.Settle();

        var detail = Assert.IsType<ValidContentDetailViewModel>(card.Detail);
        Assert.Equal(["Próbki", "rodzaj", "Goblin"], detail.Breadcrumbs);
        Assert.Equal("Wódz", detail.Name);
        Assert.Equal("Podgląd", pinned.Title);
        Assert.Equal("Podgląd", preview.Title);
    }

    [AvaloniaFact]
    public async Task A_click_on_the_title_of_a_pinned_card_expands_its_folders_and_selects_the_entity()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.CreateFolderAsync(null, "Kopalnia");
        var folder = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, folder, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        var pinned = fixture.Desk.PinEntity(id)!;

        catalog.SetExpanded(Row(catalog, "Kopalnia"), false);
        catalog.Click(catalog.Rows[0], ctrl: false, shift: false);
        Assert.DoesNotContain(catalog.Rows, row => row.Name == "Szef");

        pinned.TitleClickCommand.Execute(null);
        fixture.Settle();

        Assert.True(Row(catalog, "Kopalnia").IsExpanded);
        Assert.Equal([id], catalog.SelectedEntities);
        Assert.Equal(id, catalog.LastClickedEntity);
        Assert.Equal([Row(catalog, "Szef")], catalog.SelectedRows);
    }

    [AvaloniaFact]
    public void A_click_on_the_title_of_an_empty_preview_changes_nothing()
    {
        using var fixture = new Fixture();
        var preview = fixture.Desk.OpenPreview();

        preview.TitleClickCommand.Execute(null);

        Assert.Empty(fixture.Desk.Catalog.SelectedEntities);
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
        Assert.Equal(["pack", "gone"], broken.Breadcrumbs);
        Assert.Equal("№ 1", broken.NumberLabel);
        Assert.False(broken.HasOverline);
        Assert.Equal("Duch", broken.Name);
        Assert.Contains("nie zawiera już wpisu", broken.Reason);
        Assert.NotNull(FindView(fixture, preview));

        // The entity outlives its entry, and so does its note.
        Assert.True(broken.HasLiveSections);
        Assert.True(NoteField(fixture, preview).IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task A_creature_shows_its_live_part_an_item_its_note_and_the_library_neither()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        var tile = HitPoints(fixture, preview);
        Assert.Equal("7", tile.Value);
        Assert.Equal("7", tile.Maximum);
        Assert.Equal("2k6", tile.Note);
        var gauge = FindView(fixture, preview)!.GetVisualDescendants().OfType<TrackGauge>().Single();
        Assert.True(gauge.IsEffectivelyVisible);
        Assert.Equal(7, gauge.Value);
        Assert.Equal(7, gauge.Maximum);
        Assert.True(NoteField(fixture, preview).IsEffectivelyVisible);
        Assert.Equal(EntityLiveSectionsView.NotePlaceholder, NoteField(fixture, preview).PlaceholderText);

        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        fixture.Settle();
        Assert.True(NoteField(fixture, preview).IsEffectivelyVisible);
        Assert.Empty(FindView(fixture, preview)!.GetVisualDescendants().OfType<TrackedValueTile>());

        // The library's card of the same creature: the entry's numbers, no gauge, no note.
        var card = new CreatureCardView();
        card.SetCreature(new Creature
        {
            Size = "Mały",
            Type = "humanoid",
            Alignment = "neutralny zły",
            Combat = new CombatAspect { Ac = 15, Hp = 7, Str = 8, Dex = 14, Con = 10, Int = 10, Wis = 8, Cha = 8 },
            Speed = "9 m",
            Senses = "pasywna Percepcja 9",
            Challenge = "1/4",
            Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Maczuga", Text = "+4 do trafienia." }] },
        }, EntryPicture.None);
        var library = new Window
        {
            Width = 700,
            Height = 900,
            Content = new EntryDetailView { DataContext = new ValidContentDetailViewModel(["Próbki", "Goblin"], "Goblin", [], card) },
        };
        library.Show();
        fixture.Settle();
        Assert.Empty(library.GetVisualDescendants().OfType<TrackedValueTile>());
        Assert.Empty(library.GetVisualDescendants().OfType<EntityLiveSectionsView>());
        Assert.False(library.GetVisualDescendants().OfType<TrackGauge>().Single().IsVisible);
        library.Close();
    }

    [AvaloniaFact]
    public async Task An_accepted_change_of_the_tracked_value_is_saved_and_shown_in_both_windows_without_a_rebuild()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        var pinned = fixture.Desk.PinEntity(id)!;
        fixture.Settle();
        var card = (EntityCardViewModel)preview.Body;
        var detail = card.Detail;
        var tile = HitPoints(fixture, preview);

        NumberField(tile).Focus();
        fixture.Window.KeyTextInput("-3");
        fixture.Settle();
        Assert.Equal("7 → 4", tile.Message);
        Press(fixture.Window, Key.Enter);
        fixture.Settle();

        Assert.True(catalog.Tree.Entity(id)!.Entity.Current!.TryGetValue(CreatureValues.HitPointsKey, out var stored));
        Assert.Equal(4, stored);
        Assert.Same(detail, card.Detail);
        Assert.Same(tile, HitPoints(fixture, preview));
        Assert.Equal("4", tile.Value);
        Assert.Equal(4, FindView(fixture, preview)!.GetVisualDescendants().OfType<TrackGauge>().Single().Value);
        Assert.Equal("4", HitPoints(fixture, pinned).Value);
    }

    [AvaloniaFact]
    public async Task A_click_beside_the_open_field_drops_the_entry()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();
        var tile = HitPoints(fixture, preview);

        NumberField(tile).Focus();
        fixture.Window.KeyTextInput("-3");
        fixture.Settle();
        Click(fixture.Window, NoteField(fixture, preview));

        Assert.Null(catalog.Tree.Entity(id)!.Entity.Current);
        Assert.Equal("7", tile.Value);
        Assert.Equal("7", NumberField(tile).Text);
        Assert.Null(tile.Message);
    }

    [AvaloniaFact]
    public async Task Tab_leads_from_the_tracked_value_to_the_note_which_is_saved_when_the_field_is_left()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        fixture.Settle();
        var id = Row(catalog, "Szef").EntityId!.Value;
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();
        var note = NoteField(fixture, preview);
        var detail = ((EntityCardViewModel)preview.Body).Detail;

        NumberField(HitPoints(fixture, preview)).Focus();
        Press(fixture.Window, Key.Tab);
        Assert.True(note.IsFocused);

        fixture.Window.KeyTextInput("Boi się psów.");
        fixture.Settle();
        Assert.Null(catalog.Tree.Entity(id)!.Entity.Note);

        TopLevel.GetTopLevel(note)!.FocusManager!.Focus(null);
        fixture.Settle();
        Assert.Equal("Boi się psów.", catalog.Tree.Entity(id)!.Entity.Note);
        Assert.Same(detail, ((EntityCardViewModel)preview.Body).Detail);
    }

    [AvaloniaFact]
    public async Task A_note_being_written_is_saved_when_the_preview_moves_on_or_the_window_closes()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        var chief = Row(catalog, "Szef").EntityId!.Value;
        var rope = Row(catalog, "Lina").EntityId!.Value;
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        NoteField(fixture, preview).Focus();
        fixture.Window.KeyTextInput("Ucieknie.");
        catalog.Click(Row(catalog, "Lina"), ctrl: false, shift: false);
        fixture.Settle();
        Assert.Equal("Ucieknie.", catalog.Tree.Entity(chief)!.Entity.Note);

        var pinned = fixture.Desk.PinEntity(rope)!;
        fixture.Settle();
        NoteField(fixture, pinned).Focus();
        fixture.Window.KeyTextInput("Przecięta.");
        fixture.Desk.Close(pinned);
        fixture.Settle();
        Assert.Equal("Przecięta.", catalog.Tree.Entity(rope)!.Entity.Note);
    }

    [AvaloniaFact]
    public async Task Nothing_in_the_card_clips_the_frames_of_its_fields()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Desk.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Szef");
        fixture.Settle();
        catalog.Click(Row(catalog, "Szef"), ctrl: false, shift: false);
        var preview = fixture.Desk.OpenPreview();
        fixture.Settle();

        // The frame reaches out of the field's bounds; a clipping ancestor inside the card (a content
        // control or a user control clips by default) would cut the part that sticks out.
        foreach (var field in new[] { NumberField(HitPoints(fixture, preview)), NoteField(fixture, preview) })
        {
            var frame = field.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Frame");
            Assert.True(frame.Bounds.X < 0);
            for (var ancestor = field.GetVisualParent(); ancestor is not null; ancestor = ancestor.GetVisualParent())
            {
                if (ancestor.ClipToBounds)
                {
                    var corner = frame.TranslatePoint(default, ancestor)!.Value;
                    var area = new Rect(corner, frame.Bounds.Size);
                    Assert.True(
                        new Rect(ancestor.Bounds.Size).Contains(area),
                        $"{ancestor.GetType().Name} clips the frame of {field.Name}: {area} outside {ancestor.Bounds.Size}");
                }

                if (ancestor is EntityCardView)
                {
                    break;
                }
            }
        }
    }

    private static TrackedValueTile HitPoints(Fixture fixture, WorkspacePanelViewModel panel) =>
        FindView(fixture, panel)!.GetVisualDescendants().OfType<TrackedValueTile>().Single();

    private static EditableText NumberField(TrackedValueTile tile) =>
        tile.GetVisualDescendants().OfType<EditableText>().Single();

    private static EditableText NoteField(Fixture fixture, WorkspacePanelViewModel panel) =>
        FindView(fixture, panel)!.GetVisualDescendants().OfType<EntityLiveSectionsView>().Single()
            .GetVisualDescendants().OfType<EditableText>().Single();

    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Press(Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static WorldRowViewModel Row(WorldCatalogViewModel catalog, string name) =>
        catalog.Rows.Single(row => row.Name == name);

    private static PanelWindow Container(Fixture fixture, WorkspacePanelViewModel panel) =>
        fixture.View.GetVisualDescendants().OfType<PanelWindow>().Single(window => ReferenceEquals(window.DataContext, panel));

    private static Canvas Ghosts(Fixture fixture) =>
        fixture.View.GetVisualDescendants().OfType<Canvas>().Single(canvas => canvas.Name == "PART_Ghosts");

    private static EntityCardView? FindView(Fixture fixture, WorkspacePanelViewModel panel) =>
        fixture.View.GetVisualDescendants().OfType<EntityCardView>().SingleOrDefault(view => view.DataContext == panel.Body);

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            var system = new PreviewTestSystem();
            GameSystem = system;
            WorkspaceId = Guid.NewGuid().ToString();
            var session = new CampaignSession(
                Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System),
                new InMemoryCampaignRepository(),
                WorldModels.Combine([]));
            var source = system.GetWorldCatalogSource();
            Entries = new CampaignEntriesContext(new CampaignTabContext(session), source.Registry, source.Types);
            Store = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-preview-tests-{Guid.NewGuid():N}"));
            Desk = new CampaignWorkspaceViewModel(
                Store, WorkspaceId, WorkspaceLayout.Empty, [], () => Task.CompletedTask, Entries, system);

            View = new CampaignWorkspaceView { DataContext = Desk };
            Window = new Window { Width = 1400, Height = 900, Content = View };
            Window.Show();
            Settle();
        }

        public CampaignEntriesContext Entries { get; }

        public IGameSystem GameSystem { get; }

        public WorkspaceLayoutStore Store { get; }

        public string WorkspaceId { get; }

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
            Combat = new CombatAspect { Ac = 15, Hp = 7, HpDice = "2k6", Str = 8, Dex = 14, Con = 10, Int = 10, Wis = 8, Cha = 8 },
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
                    new ContentTabDefinition(
                        "Próbki",
                        [
                            new ContentTypeProfile<Sample>(
                                Being, new ContentCategorySpec<Sample>("Rodzaj", sample => sample.Kind), tags: sample => [sample.Kind]),
                            new ContentTypeProfile<Sample>(Thing, tags: sample => [sample.Kind]),
                        ]),
                ],
                new BeingValues());

        /// <summary>The creature's live numbers, read from the card's creature rather than the sample values.</summary>
        private sealed class BeingValues : IEntityValueCatalog, IEntityValueRules
        {
            public IEntityValueRules? For(ContentTypeReference type) => type == Being ? this : null;

            public IReadOnlyList<BaseValue> BaseValues(ContentValues values) =>
            [
                new BaseValue(CreatureValues.ArmorClassKey, "KP", Creature.Combat.Ac),
                new BaseValue(CreatureValues.MaxHitPointsKey, "PW maks.", Creature.Combat.Hp),
            ];

            public IReadOnlyList<TrackedValueRule> TrackedValues => CreatureValues.Instance.TrackedValues;
        }

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
