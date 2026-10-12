using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Workspace.World;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>Drags rows of the world catalog: the rules of a drop, and the whole gesture with real pointer events.</summary>
public sealed partial class WorldCatalogViewBuildTests
{
    [AvaloniaFact]
    public async Task Dragging_an_entity_onto_a_folder_moves_it_there_selected_with_the_folder_open()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        catalog.SetExpanded(Row(catalog, "Jaskinia"), false);
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        var to = fixture.PointOf(Row(catalog, "Jaskinia"));
        fixture.BeginDrag(Row(catalog, "Lina"), to);

        Assert.True(Row(catalog, "Jaskinia").IsDropTarget);
        Assert.True(Row(catalog, "Lina").IsDragSource);

        fixture.Window.MouseUp(to, MouseButton.Left);
        fixture.WaitUntil(() => EntityFolder(fixture) == cave);

        Assert.True(Row(catalog, "Jaskinia").IsExpanded);
        Assert.Equal(["Lina"], catalog.SelectedRows.Select(row => row.Name));
        Assert.All(catalog.Rows, row => Assert.False(row.IsDropTarget || row.IsDragSource));
    }

    [AvaloniaFact]
    public async Task Esc_cancels_a_drag_and_nothing_moves()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        var to = fixture.PointOf(Row(catalog, "Jaskinia"));
        fixture.BeginDrag(Row(catalog, "Lina"), to);
        fixture.Key(Key.Escape);
        fixture.Window.MouseUp(to, MouseButton.Left);
        fixture.Settle();

        Assert.Null(EntityFolder(fixture));
        Assert.All(catalog.Rows, row => Assert.False(row.IsDropTarget || row.IsDragSource));
    }

    [AvaloniaFact]
    public async Task The_target_frame_shows_only_where_a_drop_moves_something_and_an_entity_frames_its_folder()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.CreateFolderAsync(cave, "Skarbiec");
        await catalog.CreateFolderAsync(null, "Karczma");
        var tavern = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, tavern, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        catalog.SetExpanded(Row(catalog, "Karczma"), true);
        fixture.Settle();

        // A folder over its own child: refused, no frame anywhere.
        fixture.BeginDrag(Row(catalog, "Jaskinia"), fixture.PointOf(Row(catalog, "Skarbiec")));
        Assert.DoesNotContain(catalog.Rows, row => row.IsDropTarget);
        Assert.True(Row(catalog, "Skarbiec").IsDragSource);

        // Over an entity of another folder: that folder is framed.
        fixture.Window.MouseMove(fixture.PointOf(Row(catalog, "Goblin 2")), RawInputModifiers.LeftMouseButton);
        fixture.Settle();
        Assert.Equal(["Karczma"], catalog.Rows.Where(row => row.IsDropTarget).Select(row => row.Name));
        fixture.Key(Key.Escape);
        fixture.Window.MouseUp(fixture.PointOf(Row(catalog, "Goblin 2")), MouseButton.Left);
        fixture.Settle();

        // An entity of the root: framed folders while it would move, nothing over the root it is already in.
        fixture.BeginDrag(Row(catalog, "Lina"), fixture.PointOf(Row(catalog, "Karczma")));
        Assert.Equal(["Karczma"], catalog.Rows.Where(row => row.IsDropTarget).Select(row => row.Name));
        fixture.Window.MouseMove(fixture.PointOf(Row(catalog, "Jaskinia")), RawInputModifiers.LeftMouseButton);
        fixture.Settle();
        Assert.Equal(["Jaskinia"], catalog.Rows.Where(row => row.IsDropTarget).Select(row => row.Name));
        fixture.Window.MouseMove(fixture.PointOf(catalog.Rows[0]), RawInputModifiers.LeftMouseButton);
        fixture.Settle();
        Assert.DoesNotContain(catalog.Rows, row => row.IsDropTarget);

        // Released on a refused place: nothing moves.
        fixture.Window.MouseUp(fixture.PointOf(catalog.Rows[0]), MouseButton.Left);
        fixture.Settle();
        Assert.Null(EntityFolder(fixture, "lina"));
    }

    [AvaloniaFact]
    public async Task A_drop_carries_the_whole_selection_and_keeps_it_selected()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        catalog.Click(Row(catalog, "Goblin 2"), ctrl: false, shift: false);
        catalog.Click(Row(catalog, "Lina"), ctrl: true, shift: false);

        Assert.Equal(2, catalog.DragCount(Row(catalog, "Lina")));
        Assert.True(catalog.CanDrop(Row(catalog, "Lina"), Row(catalog, "Jaskinia")));
        Assert.False(catalog.CanDrop(Row(catalog, "Lina"), Row(catalog, "Goblin 2")));
        Assert.False(catalog.CanDrop(catalog.Rows[0], Row(catalog, "Jaskinia")));

        await catalog.DropAsync(Row(catalog, "Lina"), Row(catalog, "Jaskinia"));

        Assert.All(fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values, entity => Assert.Equal(cave, entity.FolderId));
        Assert.Equal(["Goblin 2", "Lina"], catalog.SelectedRows.Select(row => row.Name));

        // Back onto the folder they are in: refused, nothing changes.
        Assert.False(catalog.CanDrop(Row(catalog, "Lina"), Row(catalog, "Jaskinia")));
        await catalog.DropAsync(Row(catalog, "Lina"), Row(catalog, "Jaskinia"));
        Assert.All(fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values, entity => Assert.Equal(cave, entity.FolderId));
    }

    [AvaloniaFact]
    public async Task Dragging_a_row_outside_the_selection_carries_only_that_row()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        catalog.Click(Row(catalog, "Goblin 2"), ctrl: false, shift: false);

        var to = fixture.PointOf(Row(catalog, "Jaskinia"));
        fixture.BeginDrag(Row(catalog, "Lina"), to);
        Assert.False(Row(catalog, "Goblin 2").IsDragSource);
        fixture.Window.MouseUp(to, MouseButton.Left);
        fixture.WaitUntil(() => EntityFolder(fixture, "lina") == cave);

        Assert.Null(EntityFolder(fixture, "goblin"));
        Assert.Equal(["Lina"], catalog.SelectedRows.Select(row => row.Name));
    }

    private static FolderId? EntityFolder(Fixture fixture, string entry = "lina") =>
        fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values.Single(entity => entity.Source == Address(entry)).FolderId;
}
