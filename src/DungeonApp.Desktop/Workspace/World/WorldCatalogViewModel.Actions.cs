using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>What a context-menu entry of the catalog does; the view maps each to the operation that needs a window.</summary>
public enum WorldCommand
{
    Add,
    NewFolder,
    Open,
    Rename,
    MoveTo,
    Delete,
}

/// <summary>
/// One entry of the catalog's context menu. A disabled entry carries the <see cref="DisabledReason"/>
/// the view shows next to it.
/// </summary>
public sealed record WorldMenuEntry(
    WorldCommand Command,
    string Header,
    KeyGesture? Gesture = null,
    bool IsEnabled = true,
    string? DisabledReason = null,
    bool IsDanger = false);

/// <summary>The catalog's actions on the selection: menu, rename in place, delete, move.</summary>
public sealed partial class WorldCatalogViewModel
{
    private const int MaxNamesInConfirmation = 5;

    private static readonly KeyGesture AddGesture = new(Key.N, KeyModifiers.Control);
    private static readonly KeyGesture NewFolderGesture = new(Key.N, KeyModifiers.Control | KeyModifiers.Shift);
    private static readonly KeyGesture OpenGesture = new(Key.Enter);
    private static readonly KeyGesture RenameGesture = new(Key.F2);
    private static readonly KeyGesture DeleteGesture = new(Key.Delete);

    /// <summary>Raised when a rename starts, so the view can put the caret in the new field.</summary>
    public event Action<WorldRowViewModel>? RenameStarted;

    // ---- Menu ----

    /// <summary>
    /// The context menu for the current selection. The root is never a subject of rename, move or delete,
    /// so a selection of the root alone gets the add entries and a mixed selection acts on the rest.
    /// </summary>
    public IReadOnlyList<WorldMenuEntry> CreateMenu()
    {
        var subjects = SelectedRows.Where(row => !row.IsRoot).ToList();

        if (subjects.Count == 0)
        {
            return SelectedRows.Count == 0 ? [] : [AddEntry(), NewFolderEntry()];
        }

        var entries = new List<WorldMenuEntry>();
        var single = subjects.Count == 1 ? subjects[0] : null;

        if (single is { IsFolderLike: true })
        {
            entries.Add(AddEntry());
            entries.Add(NewFolderEntry());
        }

        if (single is { IsEntity: true })
        {
            entries.Add(new WorldMenuEntry(WorldCommand.Open, "Otwórz", OpenGesture));
        }

        if (single is not null)
        {
            entries.Add(new WorldMenuEntry(WorldCommand.Rename, "Zmień nazwę", RenameGesture));
        }

        entries.Add(new WorldMenuEntry(WorldCommand.MoveTo, "Przenieś do…"));

        var problem = DeleteProblem();
        entries.Add(new WorldMenuEntry(WorldCommand.Delete, "Usuń", DeleteGesture, problem is null, problem, IsDanger: true));
        return entries;

        static WorldMenuEntry AddEntry() => new(WorldCommand.Add, "Dodaj…", AddGesture);

        static WorldMenuEntry NewFolderEntry() => new(WorldCommand.NewFolder, NewFolderName, NewFolderGesture);
    }

    /// <summary>Selects <paramref name="row"/> alone unless it is already selected, as a right click does.</summary>
    public void SelectForMenu(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.IsSelected)
        {
            Click(row, ctrl: false, shift: false);
        }
    }

    // ---- Rename ----

    /// <summary>Starts renaming in place; the root cannot be renamed. The field starts with the name the row shows.</summary>
    public void BeginRename(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.IsRoot || row.IsEditing)
        {
            return;
        }

        foreach (var other in Rows.Where(candidate => candidate.IsEditing))
        {
            other.IsEditing = false;
        }

        row.EditText = row.Name;
        row.RenameError = null;
        row.IsEditing = true;
        RenameStarted?.Invoke(row);
    }

    /// <summary>
    /// Saves the typed name. A folder refuses an empty name: the field stays, with the reason on the row.
    /// An entity with an empty name goes back to its entry's name. Returns false when the name was refused.
    /// </summary>
    public async Task<bool> CommitRenameAsync(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.IsEditing)
        {
            return true;
        }

        var text = row.EditText ?? string.Empty;

        if (row.FolderId is { } folder)
        {
            if (WorldChanges.FolderNameProblem(text) is { } problem)
            {
                row.RenameError = problem;
                return false;
            }

            row.IsEditing = false;
            row.RenameError = null;

            if (text.Trim() != row.Name)
            {
                await _context.ChangeAsync(WorldChanges.RenameFolder(_context.Snapshot, folder, text));
            }

            return true;
        }

        row.IsEditing = false;
        row.RenameError = null;

        if (row.EntityId is { } id && _tree.Entity(id) is { } node
            && text != row.Name && !(string.IsNullOrWhiteSpace(text) && node.Entity.Label is null))
        {
            await _context.ChangeAsync(CampaignEntityChanges.Relabel(node.Entity, text));
        }

        return true;
    }

    /// <summary>Drops the typed name and shows the row as it was.</summary>
    public void CancelRename(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        row.IsEditing = false;
        row.RenameError = null;
    }

    // ---- Delete ----

    /// <summary>Why the selection cannot be deleted (a folder that is not empty), or null.</summary>
    public string? DeleteProblem() =>
        SelectedFolders.Select(folder => WorldChanges.DeleteFolderProblem(_context.Snapshot, folder)).FirstOrDefault(problem => problem is not null);

    /// <summary>The question for the confirmation window: a title with the count and a message naming the items.</summary>
    public (string Title, string Message) DeleteConfirmation()
    {
        var names = SelectedRows.Where(row => !row.IsRoot).Select(DisplayNameOf).ToList();
        var title = names.Count == 1 ? "Usunąć pozycję?" : $"Usunąć {names.Count} {PositionsWord(names.Count)}?";
        var listed = string.Join(", ", names.Take(MaxNamesInConfirmation));
        var message = names.Count > MaxNamesInConfirmation ? $"{listed} i {names.Count - MaxNamesInConfirmation} innych" : listed;

        return (title, message);
    }

    /// <summary>
    /// Deletes the selected entities and empty folders as one change; the selection moves to the
    /// neighbouring row, or to nothing when none is left.
    /// </summary>
    public async Task DeleteSelectionAsync()
    {
        if (DeleteProblem() is not null)
        {
            return;
        }

        var neighbour = NeighbourOfSelection();
        var change = WorldChanges.Delete(_context.Snapshot, SelectedFolders, SelectedEntities);
        var result = await _context.ChangeAsync(change);

        if (result.WasDenied)
        {
            return;
        }

        _selection.Clear();
        _anchor = null;

        if (neighbour is { } key && Rows.FirstOrDefault(row => row.Key == key) is { } row)
        {
            _selection.Add(key);
            _anchor = key;
            LastClickedEntity = row.EntityId ?? LastClickedEntity;
        }

        ApplySelection();
    }

    // The first row below the selection that is not selected, else the last one above it; never the root.
    private WorldRowKey? NeighbourOfSelection()
    {
        var selected = Rows.Select((row, index) => (row, index)).Where(pair => pair.row.IsSelected).Select(pair => pair.index).ToList();

        if (selected.Count == 0)
        {
            return null;
        }

        var below = Rows.Skip(selected[^1] + 1).FirstOrDefault(row => !row.IsSelected);
        var above = Rows.Take(selected[0]).LastOrDefault(row => !row.IsRoot && !row.IsSelected);

        return (below ?? above)?.Key;
    }

    private static string DisplayNameOf(WorldRowViewModel row) =>
        row.NumberText is { Length: > 0 } number ? $"{row.Name} {number}" : row.Name;

    private static string PositionsWord(int count) =>
        count % 100 is >= 12 and <= 14 ? "pozycji" : (count % 10) switch
        {
            1 => "pozycję",
            2 or 3 or 4 => "pozycje",
            _ => "pozycji",
        };

    // ---- Move ----

    /// <summary>Why the selection cannot go into <paramref name="target"/> (null is the root), or null.</summary>
    public string? MoveProblem(FolderId? target) =>
        WorldChanges.MoveProblem(_context.Snapshot, SelectedFolders, SelectedEntities, target);

    /// <summary>The folder a drop on <paramref name="row"/> lands in: the root's and an entity's own folder, or the folder itself.</summary>
    public FolderId? DropFolderOf(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return row.IsEntity ? _tree.Entity(row.EntityId!.Value)?.Entity.FolderId : row.FolderId;
    }

    /// <summary>
    /// Whether dragging <paramref name="dragged"/> onto <paramref name="over"/> would move something.
    /// The selection travels when the dragged row is part of it, otherwise the dragged row alone.
    /// </summary>
    public bool CanDrop(WorldRowViewModel dragged, WorldRowViewModel over)
    {
        ArgumentNullException.ThrowIfNull(dragged);
        ArgumentNullException.ThrowIfNull(over);

        if (dragged.IsRoot)
        {
            return false;
        }

        var (folders, entities) = DragSubjects(dragged);

        return WorldChanges.MoveProblem(_context.Snapshot, folders, entities, DropFolderOf(over)) is null;
    }

    /// <summary>Drops <paramref name="dragged"/> (and the selection it belongs to) on <paramref name="over"/>; nothing happens when it is refused.</summary>
    public async Task DropAsync(WorldRowViewModel dragged, WorldRowViewModel over)
    {
        if (!CanDrop(dragged, over))
        {
            return;
        }

        if (!dragged.IsSelected)
        {
            Click(dragged, ctrl: false, shift: false);
        }

        await MoveSelectionToAsync(DropFolderOf(over));
    }

    /// <summary>Moves the selected rows into <paramref name="target"/> as one change; they stay selected and the target opens.</summary>
    public async Task MoveSelectionToAsync(FolderId? target)
    {
        if (MoveProblem(target) is not null)
        {
            return;
        }

        var result = await _context.ChangeAsync(WorldChanges.Move(_context.Snapshot, SelectedFolders, SelectedEntities, target));

        if (result.WasDenied)
        {
            return;
        }

        ExpandPath(target);
        Refresh();
        LayoutChanged?.Invoke();
    }

    /// <summary>
    /// The "move to…" palette: the root and every folder the selection may go into, with the parent's
    /// path dimmed beside the name.
    /// </summary>
    public PaletteOptions CreateMoveOptions()
    {
        var subjects = SelectedRows.Where(row => !row.IsRoot).ToList();
        var what = subjects.Count == 1 ? $": {DisplayNameOf(subjects[0])}" : $" {subjects.Count}";

        return new PaletteOptions
        {
            TargetText = $"Przenieś{what} do…",
            Placeholder = "Szukaj katalogu…",
            Search = SearchFolders,
            Choose = (item, _) => MoveSelectionToAsync(((WorldAddTarget)item.Key).Folder),
        };
    }

    private IReadOnlyList<PaletteItem> SearchFolders(string query)
    {
        var text = query.Trim();
        var items = new List<PaletteItem>();

        if (MoveProblem(null) is null && Matches(RootName, text))
        {
            items.Add(new PaletteItem(new WorldAddTarget(null, RootName), FolderIconKey, RootName));
        }

        foreach (var folder in _tree.AllFolders)
        {
            if (!Matches(folder.Name, text) || MoveProblem(folder.Id) is not null)
            {
                continue;
            }

            var parent = _tree.FolderPath(folder.Id).SkipLast(1);
            items.Add(new PaletteItem(new WorldAddTarget(folder.Id, folder.Name), FolderIconKey, folder.Name, string.Join(" / ", parent)));
        }

        return items;

        static bool Matches(string name, string text) =>
            text.Length == 0 || name.Contains(text, StringComparison.CurrentCultureIgnoreCase);
    }

    private const string FolderIconKey = "DungeonIconFolder";

    private (List<FolderId> Folders, List<EntityId> Entities) DragSubjects(WorldRowViewModel dragged)
    {
        if (dragged.IsSelected)
        {
            return ([.. SelectedFolders], [.. SelectedEntities]);
        }

        return (dragged.FolderId is { } folder ? [folder] : [], dragged.EntityId is { } entity ? [entity] : []);
    }
}
