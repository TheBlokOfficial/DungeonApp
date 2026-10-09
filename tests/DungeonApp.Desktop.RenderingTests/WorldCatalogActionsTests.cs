using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Workspace.World;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>The catalog's actions on the selection: context menu, rename in place, delete, move.</summary>
public sealed partial class WorldCatalogViewBuildTests
{
    private static void Select(WorldCatalogViewModel catalog, params string[] names)
    {
        catalog.Click(Row(catalog, names[0]), ctrl: false, shift: false);

        foreach (var name in names.Skip(1))
        {
            catalog.Click(Row(catalog, name), ctrl: true, shift: false);
        }
    }

    private static IEnumerable<WorldCommand> Commands(WorldCatalogViewModel catalog) =>
        catalog.CreateMenu().Select(entry => entry.Command);

    // ---- Menu ----

    [AvaloniaFact]
    public async Task The_menu_offers_what_fits_the_selection()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Pusty");
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, cave, "Goblin");
        fixture.Settle();

        Select(catalog, "Świat");
        Assert.Equal([WorldCommand.Add, WorldCommand.NewFolder], Commands(catalog));

        Select(catalog, "Pusty");
        Assert.Equal(
            [WorldCommand.Add, WorldCommand.NewFolder, WorldCommand.Rename, WorldCommand.MoveTo, WorldCommand.Delete],
            Commands(catalog));
        Assert.True(catalog.CreateMenu().Last().IsEnabled);

        Select(catalog, "Goblin");
        Assert.Equal([WorldCommand.Open, WorldCommand.Rename, WorldCommand.MoveTo, WorldCommand.Delete], Commands(catalog));

        Select(catalog, "Pusty", "Goblin");
        Assert.Equal([WorldCommand.MoveTo, WorldCommand.Delete], Commands(catalog));
        Assert.True(catalog.CreateMenu().Last().IsEnabled);
    }

    [AvaloniaFact]
    public async Task Delete_is_disabled_with_the_reason_while_the_selection_holds_a_folder_that_is_not_empty()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, cave, "Goblin");
        fixture.Settle();
        Select(catalog, "Jaskinia");

        var delete = catalog.CreateMenu().Last();
        Assert.False(delete.IsEnabled);
        Assert.Equal("Katalog nie jest pusty.", delete.DisabledReason);

        fixture.CatalogView.RebuildMenu();
        var item = fixture.CatalogView.Menu.Items.OfType<MenuItem>().Single(candidate => (string?)candidate.Header == "Usuń");
        Assert.False(item.IsEnabled);
        Assert.Equal("Katalog nie jest pusty.", MenuItemTrailing.GetText(item));
    }

    [AvaloniaFact]
    public async Task The_built_menu_shows_the_shortcuts()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin");
        fixture.Settle();
        Select(catalog, "Goblin");

        fixture.CatalogView.RebuildMenu();

        var items = fixture.CatalogView.Menu.Items.OfType<MenuItem>().ToList();
        Assert.Equal(["Otwórz", "Zmień nazwę", "Przenieś do…", "Usuń"], items.Select(item => (string?)item.Header));
        Assert.Equal(Key.F2, items[1].InputGesture!.Key);
        Assert.Equal(Key.Delete, items[3].InputGesture!.Key);
    }

    // ---- Rename ----

    [AvaloniaFact]
    public async Task Renaming_in_place_changes_no_bounds_of_the_row_or_its_neighbours()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin");
        fixture.Settle();
        var row = Row(catalog, "Jaskinia");
        var border = fixture.RowBorders().Single(candidate => candidate.DataContext == row);
        var nameText = border.GetVisualDescendants().OfType<TextBlock>().First(text => text.Classes.Contains("world-name"));
        var field = border.GetVisualDescendants().OfType<TextBox>().Single();

        bool Inside(Visual visual) => visual == nameText || visual == field || nameText.IsVisualAncestorOf(visual) || field.IsVisualAncestorOf(visual);

        Dictionary<Visual, Rect> Layout() =>
            fixture.CatalogView.GetVisualDescendants().Where(visual => !Inside(visual)).OfType<Control>()
                .ToDictionary(visual => (Visual)visual, visual => new Rect(visual.TranslatePoint(default, fixture.CatalogView) ?? default, visual.Bounds.Size));

        fixture.Window.UpdateLayout();
        var before = Layout();
        var textBounds = new Rect(nameText.TranslatePoint(default, fixture.CatalogView)!.Value, nameText.Bounds.Size);

        catalog.BeginRename(row);
        fixture.Settle();
        fixture.Window.UpdateLayout();

        var after = Layout();
        Assert.Equal(before, after);
        Assert.True(field.IsVisible);
        Assert.False(nameText.IsVisible);

        var fieldBounds = new Rect(field.TranslatePoint(default, fixture.CatalogView)!.Value, field.Bounds.Size);
        Assert.Equal(textBounds.Right, fieldBounds.Right, 0.5);
        Assert.Equal(textBounds.Left - 1, fieldBounds.Left, 0.5);
        Assert.Equal(textBounds.Center.Y, fieldBounds.Center.Y, 1.0);
        Assert.True(fieldBounds.Height <= border.Bounds.Height);
    }

    [AvaloniaFact]
    public async Task Enter_saves_the_typed_name_and_Escape_drops_it()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        fixture.Settle();
        Select(catalog, "Jaskinia");
        fixture.CatalogView.Focus();

        fixture.Key(Key.F2);
        Assert.True(Row(catalog, "Jaskinia").IsEditing);
        Row(catalog, "Jaskinia").EditText = "Lochy";
        fixture.Key(Key.Escape);
        Assert.False(Row(catalog, "Jaskinia").IsEditing);
        Assert.Equal(["Świat", "Jaskinia"], catalog.Rows.Select(row => row.Name));

        fixture.Key(Key.F2);
        Row(catalog, "Jaskinia").EditText = "Lochy";
        fixture.Key(Key.Enter);
        fixture.WaitUntil(() => catalog.Rows.Any(row => row.Name == "Lochy"));

        Assert.False(Row(catalog, "Lochy").IsEditing);
        Assert.Equal([Row(catalog, "Lochy")], catalog.SelectedRows);
    }

    [AvaloniaFact]
    public async Task A_folder_refuses_an_empty_name_and_keeps_the_field_open_with_the_reason()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        fixture.Settle();
        var row = Row(catalog, "Jaskinia");
        catalog.BeginRename(row);
        row.EditText = "  ";

        Assert.False(await catalog.CommitRenameAsync(row));

        Assert.True(row.IsEditing);
        Assert.True(row.HasRenameError);
        Assert.Equal(WorldChanges.FolderNameProblem(""), row.RenameError);
        Assert.Equal("Jaskinia", row.Name);
    }

    [AvaloniaFact]
    public async Task An_entity_with_an_empty_name_goes_back_to_its_entry_name()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Krzywy");
        fixture.Settle();
        var row = Row(catalog, "Krzywy");

        catalog.BeginRename(row);
        Assert.Equal("Krzywy", row.EditText);
        row.EditText = string.Empty;
        Assert.True(await catalog.CommitRenameAsync(row));
        fixture.Settle();

        Assert.Equal(["Świat", "Goblin"], catalog.Rows.Select(candidate => candidate.Name));
        Assert.Null(fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values.Single().Label);
    }

    [AvaloniaFact]
    public async Task A_new_folder_from_the_keyboard_opens_its_name_for_typing()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        catalog.Click(catalog.Rows[0], ctrl: false, shift: false);
        fixture.CatalogView.Focus();

        fixture.Key(Key.N, RawInputModifiers.Control | RawInputModifiers.Shift);
        fixture.WaitUntil(() => catalog.Rows.Any(row => row.Name == "Nowy katalog"));

        var created = Row(catalog, "Nowy katalog");
        Assert.True(created.IsEditing);
        Assert.Equal("Nowy katalog", created.EditText);
        await Task.CompletedTask;
    }

    // ---- Delete ----

    [AvaloniaFact]
    public async Task Delete_asks_naming_the_items_and_removes_them_in_one_change_selecting_the_neighbour()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Skarbiec");
        await catalog.AddEntitiesAsync(Address("goblin"), 2, null);
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();
        Assert.Equal(["Świat", "Skarbiec", "Goblin", "Goblin", "Lina"], catalog.Rows.Select(row => row.Name));
        catalog.Click(catalog.Rows[2], ctrl: false, shift: false);
        catalog.Click(catalog.Rows[3], ctrl: true, shift: false);
        catalog.Click(catalog.Rows[1], ctrl: true, shift: false);

        var (title, message) = catalog.DeleteConfirmation();
        Assert.Equal("Usunąć 3 pozycje?", title);
        Assert.Equal("Skarbiec, Goblin № 1, Goblin № 2", message);

        var asked = fixture.CatalogView.ExecuteAsync(WorldCommand.Delete);
        fixture.Settle();
        var confirm = fixture.Overlay.GetVisualDescendants().OfType<Button>().Single(button => button.Name == ConfirmationDialog.ConfirmButtonPart);
        confirm.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await asked;
        fixture.Settle();

        Assert.Equal(["Świat", "Lina"], catalog.Rows.Select(row => row.Name));
        Assert.Equal([Row(catalog, "Lina")], catalog.SelectedRows);
        Assert.Empty(fixture.Entries.Snapshot.Get(WorldModels.Folders));
    }

    [AvaloniaFact]
    public async Task Cancelling_the_question_deletes_nothing()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        fixture.Settle();

        var asked = fixture.CatalogView.ExecuteAsync(WorldCommand.Delete);
        fixture.Settle();
        var cancel = fixture.Overlay.GetVisualDescendants().OfType<Button>().Single(button => button.Name == ConfirmationDialog.CancelButtonPart);
        cancel.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await asked;

        Assert.Equal(2, catalog.Rows.Count);
    }

    [AvaloniaFact]
    public void A_long_list_of_names_is_shortened()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        var added = catalog.AddEntitiesAsync(Address("goblin"), 12, null);
        fixture.WaitUntil(() => added.IsCompleted);

        var (title, message) = catalog.DeleteConfirmation();

        Assert.Equal("Usunąć 12 pozycji?", title);
        Assert.EndsWith(" i 7 innych", message);
        Assert.Equal(5, message.Split(',').Length);
    }

    // ---- Move ----

    [AvaloniaFact]
    public async Task Dropping_a_row_moves_the_selection_it_belongs_to_and_opens_the_target()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 1");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin 2");
        await catalog.AddEntitiesAsync(Address("lina"), 1, null);
        catalog.SetExpanded(Row(catalog, "Jaskinia"), false);
        fixture.Settle();

        Select(catalog, "Goblin 1", "Goblin 2");
        Assert.True(catalog.CanDrop(Row(catalog, "Goblin 1"), Row(catalog, "Jaskinia")));
        await catalog.DropAsync(Row(catalog, "Goblin 1"), Row(catalog, "Jaskinia"));
        fixture.Settle();

        Assert.True(Row(catalog, "Jaskinia").IsExpanded);
        Assert.Equal(["Goblin 1", "Goblin 2"], catalog.SelectedRows.Select(row => row.Name));
        Assert.All(catalog.SelectedEntities, id => Assert.Equal(cave, fixture.Entries.Snapshot.Get(EntitiesModel.Declaration)[id.ToString()].FolderId));

        // An unselected row dragged alone moves alone; dropping on an entity lands in its folder, on the root in the root.
        await catalog.DropAsync(Row(catalog, "Lina"), Row(catalog, "Goblin 1"));
        Assert.Equal([Row(catalog, "Lina")], catalog.SelectedRows);
        Assert.Equal(cave, fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values.Single(entity => entity.Label is null).FolderId);

        await catalog.DropAsync(Row(catalog, "Goblin 2"), catalog.Rows[0]);
        Assert.Null(fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values.Single(entity => entity.Label == "Goblin 2").FolderId);
    }

    [AvaloniaFact]
    public async Task A_folder_cannot_be_dropped_into_itself_or_its_descendant_and_the_root_cannot_be_dragged()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.CreateFolderAsync(cave, "Skarbiec");
        fixture.Settle();
        var before = fixture.Entries.Snapshot;

        Assert.False(catalog.CanDrop(Row(catalog, "Jaskinia"), Row(catalog, "Jaskinia")));
        Assert.False(catalog.CanDrop(Row(catalog, "Jaskinia"), Row(catalog, "Skarbiec")));
        Assert.False(catalog.CanDrop(catalog.Rows[0], Row(catalog, "Jaskinia")));
        await catalog.DropAsync(Row(catalog, "Jaskinia"), Row(catalog, "Skarbiec"));

        Assert.Same(before, fixture.Entries.Snapshot);
        Select(catalog, "Jaskinia");
        Assert.NotNull(catalog.MoveProblem(cave));
    }

    [AvaloniaFact]
    public async Task Move_to_lists_only_folders_the_selection_may_enter_and_keeps_it_selected()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.CreateFolderAsync(cave, "Skarbiec");
        await catalog.CreateFolderAsync(null, "Wieża");
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin");
        fixture.Settle();

        Select(catalog, "Jaskinia");
        var options = catalog.CreateMoveOptions();
        Assert.Equal("Przenieś: Jaskinia do…", options.TargetText);
        Assert.Equal(["Świat", "Wieża"], options.Search(string.Empty).Select(item => item.Name));

        Select(catalog, "Goblin");
        Assert.Equal("Przenieś: Goblin № 1 do…", catalog.CreateMoveOptions().TargetText);
        var all = catalog.CreateMoveOptions().Search(string.Empty);
        Assert.Equal(["Świat", "Jaskinia", "Skarbiec", "Wieża"], all.Select(item => item.Name));
        Assert.Equal("Jaskinia", all.Single(item => item.Name == "Skarbiec").Tags);
        Assert.All(all, item => Assert.Equal("DungeonIconFolder", item.IconKey));

        catalog.Click(Row(catalog, "Goblin"), ctrl: false, shift: false);
        catalog.Click(Row(catalog, "Wieża"), ctrl: true, shift: false);
        Assert.Equal("Przenieś 2 do…", catalog.CreateMoveOptions().TargetText);
    }

    [AvaloniaFact]
    public async Task Choosing_a_folder_in_the_move_palette_moves_the_selection_there()
    {
        using var fixture = new Fixture();
        var catalog = fixture.Catalog;
        await catalog.CreateFolderAsync(null, "Jaskinia");
        var cave = catalog.SelectedFolders.Single();
        await catalog.AddEntitiesAsync(Address("goblin"), 1, null, "Goblin");
        catalog.SetExpanded(Row(catalog, "Jaskinia"), false);
        fixture.Settle();
        Select(catalog, "Goblin");
        fixture.CatalogView.Focus();

        _ = fixture.CatalogView.ExecuteAsync(WorldCommand.MoveTo);
        fixture.Settle();
        var palette = fixture.Overlay.Children.OfType<Palette>().Single();
        Assert.Equal("Przenieś: Goblin № 1 do…", palette.TargetText);
        fixture.Type("jask");
        Assert.Equal(["Jaskinia"], palette.Rows.Select(row => row.DisplayName));
        fixture.Key(Key.Enter);
        fixture.WaitUntil(() => fixture.Entries.Snapshot.Get(EntitiesModel.Declaration).Values.Single().FolderId == cave);

        Assert.True(Row(catalog, "Jaskinia").IsExpanded);
        Assert.Equal(["Goblin"], catalog.SelectedRows.Select(row => row.Name));
    }
}
