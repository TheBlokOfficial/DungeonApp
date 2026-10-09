using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Core.Entries;
using DungeonApp.Core.World;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>What a tree row stands for.</summary>
public enum WorldRowKind
{
    /// <summary>The catalog's root, "Świat": not a record, always the first row.</summary>
    Root,
    Folder,
    Entity,
}

/// <summary>
/// The identity of a tree row: a folder, an entity, or neither (the root). Stable across refreshes,
/// so selection and the row objects survive a change notification.
/// </summary>
public readonly record struct WorldRowKey(FolderId? Folder, EntityId? Entity)
{
    public static WorldRowKey Root => default;

    public static WorldRowKey ForFolder(FolderId id) => new(id, null);

    public static WorldRowKey ForEntity(EntityId id) => new(null, id);

    public WorldRowKind Kind => Entity is not null ? WorldRowKind.Entity : Folder is not null ? WorldRowKind.Folder : WorldRowKind.Root;
}

/// <summary>
/// One visible row of the world tree. The tree builds these and keeps the same object for the same
/// <see cref="Key"/> across refreshes, only updating what changed.
/// </summary>
public sealed partial class WorldRowViewModel(WorldRowKey key) : ObservableObject
{
    private const double IndentPerLevel = 16;

    public WorldRowKey Key { get; } = key;

    public WorldRowKind Kind => Key.Kind;

    public bool IsRoot => Kind == WorldRowKind.Root;

    public bool IsEntity => Kind == WorldRowKind.Entity;

    /// <summary>A folder or the root: has a chevron, can be expanded and added to.</summary>
    public bool IsFolderLike => Kind != WorldRowKind.Entity;

    /// <summary>The folder this row is, or null for the root and for an entity.</summary>
    public FolderId? FolderId => Key.Folder;

    /// <summary>The entity this row is, or null for a folder.</summary>
    public EntityId? EntityId => Key.Entity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Indent))]
    public partial int Depth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>"№ 5" for an entity, null for a folder.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberSuffix))]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    public partial string? NumberText { get; set; }

    /// <summary>The system's short text at the row's right edge (health), null when it has none.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHint))]
    public partial string? Hint { get; set; }

    /// <summary>Resource key of the entity's type icon; null for a folder or a type the system does not list.</summary>
    [ObservableProperty]
    public partial string? IconKey { get; set; }

    /// <summary>An entity whose entry is gone or does not resolve: the row shows a warning instead of the type icon.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsTypeIcon))]
    public partial bool IsUnresolved { get; set; }

    /// <summary>An entity that resolves shows its type's icon.</summary>
    public bool ShowsTypeIcon => IsEntity && !IsUnresolved;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public bool HasHint => Hint is { Length: > 0 };

    /// <summary>The number as it follows the name on the row, dimmed by the view; empty for a folder.</summary>
    public string NumberSuffix => NumberText is { Length: > 0 } number ? "  " + number : string.Empty;

    /// <summary>The whole row's name for the tooltip, which a trimmed row cannot show.</summary>
    public string ToolTipText => NumberText is { Length: > 0 } number ? $"{Name} ({number})" : Name;

    /// <summary>Left margin that shows the row's depth in the tree.</summary>
    public Thickness Indent => new(Depth * IndentPerLevel, 0, 0, 0);
}
