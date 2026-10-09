using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Core.Entries;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Controls;
using DungeonApp.Desktop.Entries;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Controls;
using DungeonApp.Desktop.Workspace.Layout;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>Where a palette adds to: a folder (null is the root) and the name the palette shows for it.</summary>
public readonly record struct WorldAddTarget(FolderId? Folder, string Name);

/// <summary>
/// The world catalog lying on the desk: the tree of folders and entities as a flat list of visible
/// rows, the selection, and where the catalog stands. Reads the tree from the campaign's snapshot
/// (<see cref="WorldTree"/>) on every change notification and writes only through the context's
/// one door, one GM action as one change.
/// <para>
/// What the next features hook into: <see cref="SelectedRows"/> and <see cref="LastClickedEntity"/>
/// for the selection, <see cref="OpenEntityRequested"/> (double click or Enter on an entity) for the
/// preview, and the public operations (<see cref="Click"/>, <see cref="Activate"/>,
/// <see cref="ResolveAddTarget"/>, <see cref="CreateAddOptions"/>, <see cref="AddEntitiesAsync"/>,
/// <see cref="CreateFolderAsync"/>) for menus and keys.
/// </para>
/// </summary>
public sealed partial class WorldCatalogViewModel : ObservableObject, IDisposable
{
    /// <summary>Height of the root row, which the catalog occupies alone while collapsed; used to snap and clamp a move.</summary>
    public const double HeaderHeight = 24;

    /// <summary>The root's name, which is also how the add palette calls it.</summary>
    public const string RootName = "Świat";

    /// <summary>The name Ctrl+Shift+N gives a new folder.</summary>
    public const string NewFolderName = "Nowy katalog";

    /// <summary>Most results one palette search lists, so a long pack does not build thousands of rows per keystroke.</summary>
    private const int MaxSearchResults = 100;

    private readonly CampaignEntriesContext _context;
    private readonly IGameSystem _system;
    private readonly WorldCatalogSource _source;
    private readonly Dictionary<WorldRowKey, WorldRowViewModel> _rows = [];
    private readonly HashSet<WorldRowKey> _selection = [];
    private readonly HashSet<FolderId> _expanded;

    private WorldTree _tree;
    private WorldRowKey? _anchor;
    private double? _desiredX;
    private double? _desiredY;
    private double _surfaceWidth;
    private double _surfaceHeight;
    private bool _hasSurface;
    private WorkspaceMetrics _metrics = WorkspaceMetrics.Fallback;
    private bool _isDisposed;

    public WorldCatalogViewModel(CampaignEntriesContext context, IGameSystem system, WorldCatalogLayout? layout)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(system);

        _context = context;
        _system = system;
        _source = system.GetWorldCatalogSource();
        _tree = WorldTree.Read(context.Snapshot, context.Resolver);

        _desiredX = layout?.X;
        _desiredY = layout?.Y;
        Width = ClampWidth(layout is { Width: > 0 } ? layout.Width : WorkspaceGridSettings.CatalogDefaultWidth);
        IsRootCollapsed = layout?.IsRootCollapsed ?? false;
        _expanded = [.. (layout?.ExpandedFolders ?? []).Select(TryParseFolder).OfType<FolderId>()];

        _context.Changed += OnChanged;
        Refresh();
        Fit();
    }

    /// <summary>The visible rows in display order: the root, then, while it is expanded, its folders and entities.</summary>
    public ObservableCollection<WorldRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    public partial double Width { get; private set; }

    /// <summary>The collapsed root is the catalog's single line "▸ Świat".</summary>
    [ObservableProperty]
    public partial bool IsRootCollapsed { get; private set; }

    /// <summary>Effective position on the desk (the desired one clamped into it).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Margin))]
    public partial double X { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Margin))]
    public partial double Y { get; private set; }

    /// <summary>Places the catalog on the desk: its top-left corner at <see cref="X"/>, <see cref="Y"/>, reaching down to the desk's bottom.</summary>
    public Thickness Margin => new(X, Y, 0, 0);

    /// <summary>The entity the GM clicked last, null while none is, or after it was removed. The preview follows this one.</summary>
    [ObservableProperty]
    public partial EntityId? LastClickedEntity { get; private set; }

    /// <summary>The selected rows in display order (hidden ones, inside a collapsed folder, are not listed).</summary>
    public IReadOnlyList<WorldRowViewModel> SelectedRows => [.. Rows.Where(row => row.IsSelected)];

    /// <summary>The selected folders; the root is never among them.</summary>
    public IReadOnlyList<FolderId> SelectedFolders => [.. _selection.Select(key => key.Folder).OfType<FolderId>()];

    /// <summary>The selected entities.</summary>
    public IReadOnlyList<EntityId> SelectedEntities => [.. _selection.Select(key => key.Entity).OfType<EntityId>()];

    /// <summary>Raised when the GM opens an entity (double click or Enter on its row).</summary>
    public event Action<EntityId>? OpenEntityRequested;

    /// <summary>The tree as of the last change notification - what a window showing an entity reads its card from.</summary>
    public WorldTree Tree => _tree;

    /// <summary>Raised after the tree was read again, so windows showing an entity can follow a rename or a removal.</summary>
    public event Action? TreeChanged;

    /// <summary>Raised when something the desk layout stores changed (place, width, expansion).</summary>
    public event Action? LayoutChanged;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _context.Changed -= OnChanged;
    }

    /// <summary>The desk's size and measurements; keeps the catalog inside the desk and finds its default place.</summary>
    public void SetSurface(double width, double height, WorkspaceMetrics metrics)
    {
        _surfaceWidth = width;
        _surfaceHeight = height;
        _metrics = metrics;
        _hasSurface = true;
        Fit();
    }

    /// <summary>The layout this catalog saves with the desk: only folders that still exist are remembered as expanded.</summary>
    public WorldCatalogLayout CreateLayout() =>
        new(
            _desiredX,
            _desiredY,
            Width,
            IsRootCollapsed,
            [.. _expanded.Where(id => _tree.Folder(id) is not null).Select(id => id.ToString())]);

    // ---- Moving and resizing ----

    /// <summary>
    /// Follows a drag of the root row: the catalog goes where the pointer took it, kept inside the
    /// desk. <paramref name="commit"/> ends the gesture and snaps to the grid like a window does.
    /// </summary>
    public void MoveTo(double x, double y, bool commit)
    {
        var raw = new PanelPlacement(x, y, Width, HeaderHeight);
        var placed = commit
            ? PanelGeometry.SnapMove(raw, _surfaceWidth, _surfaceHeight, [], _metrics, WorkspaceGridSettings.DefaultSnapStep)
            : PanelGeometry.ClampMove(raw, _surfaceWidth, _surfaceHeight, _metrics);

        X = placed.X;
        Y = placed.Y;

        if (commit)
        {
            _desiredX = placed.X;
            _desiredY = placed.Y;
            LayoutChanged?.Invoke();
        }
    }

    /// <summary>Follows a drag of the right edge; <paramref name="commit"/> snaps the width to the grid.</summary>
    public void ResizeTo(double width, bool commit)
    {
        if (commit)
        {
            width = PanelGeometry.SnapToGrid(width, WorkspaceGridSettings.DefaultSnapStep);
        }

        // Pin the position first: the default place hangs on the desk's right edge, so a wider
        // catalog would otherwise slide left under the pointer that is widening it.
        _desiredX ??= X;
        _desiredY ??= Y;
        Width = ClampWidth(width);
        Fit();

        if (commit)
        {
            LayoutChanged?.Invoke();
        }
    }

    /// <summary>Back to the default place (the desk's top-right corner) and the default width.</summary>
    public void ResetPlacement()
    {
        _desiredX = null;
        _desiredY = null;
        Width = ClampWidth(WorkspaceGridSettings.CatalogDefaultWidth);
        Fit();
    }

    /// <summary>Fixes the position before a drag starts, so the first move starts from where the catalog stands.</summary>
    public void BeginMove()
    {
        _desiredX ??= X;
        _desiredY ??= Y;
    }

    // ---- Selection ----

    /// <summary>
    /// A click on <paramref name="row"/>: alone it selects only that row; with Ctrl it adds or removes
    /// it; with Shift it selects the range from the last plain click to this row in display order.
    /// </summary>
    public void Click(WorldRowViewModel row, bool ctrl, bool shift)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (shift && _anchor is { } anchor && Rows.FirstOrDefault(candidate => candidate.Key == anchor) is { } from)
        {
            var start = Rows.IndexOf(from);
            var end = Rows.IndexOf(row);
            var range = Rows.Skip(Math.Min(start, end)).Take(Math.Abs(end - start) + 1).Select(candidate => candidate.Key);

            if (!ctrl)
            {
                _selection.Clear();
            }

            _selection.UnionWith(range);
        }
        else if (ctrl)
        {
            if (!_selection.Remove(row.Key))
            {
                _selection.Add(row.Key);
            }

            _anchor = row.Key;
        }
        else
        {
            _selection.Clear();
            _selection.Add(row.Key);
            _anchor = row.Key;
        }

        if (row.EntityId is { } entity && _selection.Contains(row.Key))
        {
            LastClickedEntity = entity;
        }

        ApplySelection();
    }

    /// <summary>Moves the selection to the row <paramref name="delta"/> places from the last clicked one (arrow keys).</summary>
    public void MoveSelection(int delta, bool extend)
    {
        if (Rows.Count == 0)
        {
            return;
        }

        var from = _anchor is { } anchor ? Rows.FirstOrDefault(candidate => candidate.Key == anchor) : null;
        var index = from is null ? (delta > 0 ? 0 : Rows.Count - 1) : Math.Clamp(Rows.IndexOf(from) + delta, 0, Rows.Count - 1);

        if (extend && from is not null)
        {
            _selection.Add(Rows[index].Key);
            ApplySelection();
            return;
        }

        Click(Rows[index], ctrl: false, shift: false);
    }

    /// <summary>The row that Enter and the arrow keys act on: the last clicked one while it is selected, else the first selected.</summary>
    public WorldRowViewModel? PrimaryRow =>
        (_anchor is { } anchor ? Rows.FirstOrDefault(row => row.Key == anchor && row.IsSelected) : null)
        ?? Rows.FirstOrDefault(row => row.IsSelected);

    /// <summary>Double click or Enter: an entity is opened, a folder is expanded or collapsed.</summary>
    public void Activate(WorldRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (row.EntityId is { } entity)
        {
            LastClickedEntity = entity;
            OpenEntityRequested?.Invoke(entity);
            return;
        }

        ToggleExpanded(row);
    }

    public void ToggleExpanded(WorldRowViewModel row) => SetExpanded(row, !row.IsExpanded);

    /// <summary>Expands or collapses a folder (or the root); an entity has nothing to expand.</summary>
    public void SetExpanded(WorldRowViewModel row, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.IsFolderLike || row.IsExpanded == expanded)
        {
            return;
        }

        if (row.FolderId is { } folder)
        {
            if (expanded)
            {
                _expanded.Add(folder);
            }
            else
            {
                _expanded.Remove(folder);
            }
        }
        else
        {
            IsRootCollapsed = !expanded;
        }

        Refresh();
        LayoutChanged?.Invoke();
    }

    // ---- Adding ----

    /// <summary>
    /// Where Ctrl+N adds: the selected folder, else the folder of the selected entity, else the root.
    /// </summary>
    public WorldAddTarget ResolveAddTarget() => PrimaryRow is { } row ? AddTargetOf(row) : AddTargetOf(null);

    /// <summary>The target of the "+" in <paramref name="row"/>, or of an entity row: its own folder.</summary>
    public WorldAddTarget AddTargetOf(WorldRowViewModel? row)
    {
        FolderId? folder = row switch
        {
            null => null,
            { IsFolderLike: true } => row.FolderId,
            _ => _tree.Entity(row.EntityId!.Value)?.Entity.FolderId,
        };

        return new WorldAddTarget(folder, folder is { } id ? _tree.Folder(id)?.Name ?? RootName : RootName);
    }

    /// <summary>
    /// What the add palette shows and does for <paramref name="target"/>: the entries whose type the
    /// system lists as having entities, "N name" for several at once, Ctrl+Enter to keep it open.
    /// </summary>
    public PaletteOptions CreateAddOptions(WorldAddTarget target) =>
        new()
        {
            TargetText = $"Dodaj do: {target.Name}",
            Placeholder = "Szukaj wpisu…",
            Hint = "Liczba przed nazwą dodaje kilka sztuk: 4 gob",
            AllowQuantity = true,
            AllowKeepOpen = true,
            Search = SearchEntries,
            Choose = (item, quantity) => AddEntitiesAsync((EntryAddress)item.Key, quantity, target.Folder),
        };

    /// <summary>
    /// Adds <paramref name="count"/> entities of <paramref name="source"/> to <paramref name="folder"/>
    /// (null is the root) as one change, selects them and opens their folder.
    /// </summary>
    public async Task AddEntitiesAsync(EntryAddress source, int count, FolderId? folder, string? label = null)
    {
        var added = WorldChanges.AddEntities(_context.Snapshot, source, label, count, folder);
        var result = await _context.ChangeAsync(added.Change);

        if (result.WasDenied)
        {
            return;
        }

        ExpandPath(folder);
        _selection.Clear();
        _selection.UnionWith(added.Ids.Select(WorldRowKey.ForEntity));
        _anchor = WorldRowKey.ForEntity(added.Ids[^1]);
        LastClickedEntity = added.Ids[^1];
        Refresh();
        LayoutChanged?.Invoke();
    }

    /// <summary>A new folder named <paramref name="name"/> inside <paramref name="parent"/> (null is the root), selected.</summary>
    public async Task CreateFolderAsync(FolderId? parent, string name)
    {
        var added = WorldChanges.CreateFolder(_context.Snapshot, parent, name);
        var result = await _context.ChangeAsync(added.Change);

        if (result.WasDenied)
        {
            return;
        }

        ExpandPath(parent);
        _selection.Clear();
        _selection.Add(WorldRowKey.ForFolder(added.Id));
        _anchor = WorldRowKey.ForFolder(added.Id);
        Refresh();
        LayoutChanged?.Invoke();
    }

    /// <summary>Ctrl+Shift+N: a folder "Nowy katalog" where Ctrl+N would add, its name open for typing.</summary>
    public async Task CreateFolderInTargetAsync()
    {
        await CreateFolderAsync(ResolveAddTarget().Folder, NewFolderName);

        if (PrimaryRow is { FolderId: not null } created)
        {
            BeginRename(created);
        }
    }

    // ---- Reading the tree ----

    private void OnChanged(DungeonApp.Core.State.CampaignStateSnapshot snapshot) => Refresh();

    private IReadOnlyList<PaletteItem> SearchEntries(string query)
    {
        var types = _system.EntityTypes.ToDictionary(type => type.Type);
        var profiles = _source.Profiles.GroupBy(profile => profile.Type).ToDictionary(group => group.Key, group => group.First());
        var text = query.Trim();

        return
        [
            .. _context.Registry.Entries
                .Where(entry => entry.Unresolved is null
                    && entry.Type is { } descriptor
                    && types.ContainsKey(descriptor.Reference)
                    && (text.Length == 0 || entry.Entry.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)))
                .OrderBy(entry => entry.Entry.Name, NaturalTextComparer.Instance)
                .Take(MaxSearchResults)
                .Select(entry => ToPaletteItem(entry, types[entry.Type!.Value.Reference], profiles)),
        ];
    }

    private PaletteItem ToPaletteItem(
        RegisteredEntry entry, WorldEntityType type, Dictionary<ContentTypeReference, IContentTypeProfile> profiles)
    {
        if (!profiles.TryGetValue(type.Type, out var profile))
        {
            return new PaletteItem(entry.Address, type.IconResourceKey, entry.Entry.Name);
        }

        var badge = profile.Badge(entry.Entry);
        var brush = badge.ColorKey is { } colorKey ? _source.Presentation.ResolveBadgeBrush(colorKey) : null;

        return new PaletteItem(
            entry.Address,
            type.IconResourceKey,
            entry.Entry.Name,
            string.Join(", ", profile.Tags(entry.Entry)),
            badge.Text is { Length: > 0 } text ? text : null,
            brush);
    }

    private void ExpandPath(FolderId? folder)
    {
        IsRootCollapsed = false;

        // The snapshot already holds the folder chain even when the change just added a child of it.
        var tree = WorldTree.Read(_context.Snapshot, _context.Resolver);
        var visited = new HashSet<FolderId>();

        for (var at = folder; at is { } id && visited.Add(id); at = tree.Folder(id)?.ParentId)
        {
            _expanded.Add(id);
        }
    }

    private void Refresh()
    {
        if (_isDisposed)
        {
            return;
        }

        _tree = WorldTree.Read(_context.Snapshot, _context.Resolver);

        var built = new List<WorldRowViewModel> { BuildRoot() };

        if (!IsRootCollapsed)
        {
            AddChildren(built, null, depth: 1);
        }

        // Selection of something that no longer exists goes with it.
        _selection.RemoveWhere(key => key.Entity is { } entity ? _tree.Entity(entity) is null : key.Folder is { } folder && _tree.Folder(folder) is null);

        if (LastClickedEntity is { } clicked && _tree.Entity(clicked) is null)
        {
            LastClickedEntity = null;
        }

        var live = built.Select(row => row.Key).ToHashSet();
        foreach (var stale in _rows.Keys.Where(key => !live.Contains(key)).ToList())
        {
            _rows.Remove(stale);
        }

        SyncRows(built);
        ApplySelection();
        TreeChanged?.Invoke();
    }

    private WorldRowViewModel BuildRoot()
    {
        var row = RowFor(WorldRowKey.Root);
        row.Name = RootName;
        row.Depth = 0;
        row.IsExpanded = !IsRootCollapsed;
        return row;
    }

    private void AddChildren(List<WorldRowViewModel> into, FolderId? parent, int depth)
    {
        foreach (var node in _tree.Children(parent))
        {
            switch (node)
            {
                case FolderNode folderNode:
                {
                    var row = RowFor(WorldRowKey.ForFolder(folderNode.Folder.Id));
                    row.Name = folderNode.Folder.Name;
                    row.Depth = depth;
                    row.IsExpanded = _expanded.Contains(folderNode.Folder.Id);
                    into.Add(row);

                    if (row.IsExpanded)
                    {
                        AddChildren(into, folderNode.Folder.Id, depth + 1);
                    }

                    break;
                }

                case EntityNode entityNode:
                {
                    var row = RowFor(WorldRowKey.ForEntity(entityNode.Entity.Id));
                    row.Name = entityNode.Name;
                    row.Depth = depth;
                    row.NumberText = $"№ {entityNode.Entity.Number.ToString(CultureInfo.CurrentCulture)}";
                    row.IsUnresolved = entityNode.Resolved.Unresolved is not null;
                    row.Hint = row.IsUnresolved ? null : _system.RowHint(entityNode.Resolved);
                    row.IconKey = entityNode.Resolved.Source?.Entry.Type is { } type
                        ? _system.EntityTypes.FirstOrDefault(candidate => candidate.Type == type)?.IconResourceKey
                        : null;
                    into.Add(row);
                    break;
                }
            }
        }
    }

    private WorldRowViewModel RowFor(WorldRowKey key)
    {
        if (!_rows.TryGetValue(key, out var row))
        {
            row = new WorldRowViewModel(key);
            _rows[key] = row;
        }

        return row;
    }

    /// <summary>Brings <see cref="Rows"/> to <paramref name="wanted"/> by removing, inserting and moving, so rows that stay keep their containers.</summary>
    private void SyncRows(List<WorldRowViewModel> wanted)
    {
        var keep = wanted.ToHashSet();

        for (var index = Rows.Count - 1; index >= 0; index--)
        {
            if (!keep.Contains(Rows[index]))
            {
                Rows.RemoveAt(index);
            }
        }

        for (var index = 0; index < wanted.Count; index++)
        {
            if (index < Rows.Count && ReferenceEquals(Rows[index], wanted[index]))
            {
                continue;
            }

            var at = Rows.IndexOf(wanted[index]);

            if (at >= 0)
            {
                Rows.Move(at, index);
            }
            else
            {
                Rows.Insert(index, wanted[index]);
            }
        }
    }

    private void ApplySelection()
    {
        foreach (var row in Rows)
        {
            row.IsSelected = _selection.Contains(row.Key);
        }

        OnPropertyChanged(nameof(SelectedRows));
        OnPropertyChanged(nameof(SelectedFolders));
        OnPropertyChanged(nameof(SelectedEntities));
    }

    private void Fit()
    {
        if (!_hasSurface)
        {
            X = _desiredX ?? 0;
            Y = _desiredY ?? 0;
            return;
        }

        var step = WorkspaceGridSettings.DefaultSnapStep;
        var x = _desiredX ?? PanelGeometry.SnapToGrid(_surfaceWidth - Width - WorkspaceGridSettings.CatalogEdgeInset, step);
        var y = _desiredY ?? WorkspaceGridSettings.CatalogEdgeInset;
        var placed = PanelGeometry.ClampMove(new PanelPlacement(x, y, Width, HeaderHeight), _surfaceWidth, _surfaceHeight, _metrics);

        X = placed.X;
        Y = placed.Y;
    }

    private static double ClampWidth(double width) =>
        Math.Clamp(width, WorkspaceGridSettings.CatalogMinWidth, WorkspaceGridSettings.CatalogMaxWidth);

    private static FolderId? TryParseFolder(string text) =>
        Guid.TryParse(text, out var guid) ? new FolderId(guid) : null;
}
