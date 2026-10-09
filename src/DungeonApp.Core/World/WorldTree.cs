using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Core.World;

/// <summary>One child in a folder of the tree: either a folder or an entity.</summary>
public abstract record WorldNode;

/// <summary>A folder as a child of another folder (or of the root).</summary>
public sealed record FolderNode(WorldFolder Folder) : WorldNode;

/// <summary>
/// An entity as a child of a folder. <see cref="Name"/> is what the tree shows and sorts by: the
/// GM's label, else the entry's name, else - when the entry has vanished from its pack - the last
/// segment of its address (the entry id), so a row always has a name.
/// </summary>
public sealed record EntityNode(CampaignEntity Entity, string Name, ResolvedEntity Resolved) : WorldNode;

/// <summary>
/// The world tree as a view reads it, built from one snapshot: children of a folder in display order
/// and the paths of folders and entities. Read-only and rebuilt on every change notification - it is
/// not state. A folder or entity pointing at a folder that does not exist is shown in the root
/// rather than lost.
/// </summary>
public sealed class WorldTree
{
    private readonly Dictionary<FolderId, WorldFolder> _folders;
    private readonly Dictionary<EntityId, EntityNode> _entities;
    // Keyed by the folder, with the default (empty) id standing for the root.
    private readonly Dictionary<FolderId, List<WorldNode>> _children = [];

    private WorldTree(Dictionary<FolderId, WorldFolder> folders, Dictionary<EntityId, EntityNode> entities)
    {
        _folders = folders;
        _entities = entities;

        foreach (var group in folders.Values.GroupBy(folder => Key(folder.ParentId)))
        {
            _children[group.Key] =
            [
                .. group
                    .OrderBy(folder => folder.Name, NaturalTextComparer.Instance)
                    .ThenBy(folder => folder.Id.Value)
                    .Select(folder => (WorldNode)new FolderNode(folder)),
            ];
        }

        foreach (var group in entities.Values.GroupBy(node => Key(node.Entity.FolderId)))
        {
            var folderNodes = _children.GetValueOrDefault(group.Key) ?? [];

            folderNodes.AddRange(group
                .OrderBy(node => node.Name, NaturalTextComparer.Instance)
                .ThenBy(node => node.Entity.Number));
            _children[group.Key] = folderNodes;
        }
    }

    public static WorldTree Read(CampaignStateSnapshot snapshot, EntityResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(resolver);

        var folders = snapshot.Get(WorldModels.Folders).Values.ToDictionary(folder => folder.Id);
        var entities = snapshot.Get(EntitiesModel.Declaration).Values
            .Select(entity => BuildNode(entity, resolver))
            .ToDictionary(node => node.Entity.Id);

        return new WorldTree(folders, entities);
    }

    /// <summary>Every folder, ordered by its path - what a "move to…" list shows.</summary>
    public IReadOnlyList<WorldFolder> AllFolders =>
        [.. _folders.Values.OrderBy(folder => string.Join('/', FolderPath(folder.Id)), NaturalTextComparer.Instance)];

    /// <summary>The children of <paramref name="folder"/> (null is the root): folders first, then entities, each alphabetically with numbers read as numbers.</summary>
    public IReadOnlyList<WorldNode> Children(FolderId? folder) =>
        _children.TryGetValue(Key(folder), out var nodes) ? nodes : [];

    public WorldFolder? Folder(FolderId id) => _folders.GetValueOrDefault(id);

    public EntityNode? Entity(EntityId id) => _entities.GetValueOrDefault(id);

    /// <summary>Folder names from the root down to <paramref name="folder"/>, the root itself not included; empty for the root.</summary>
    public IReadOnlyList<string> FolderPath(FolderId? folder)
    {
        var names = new List<string>();
        var visited = new HashSet<FolderId>();

        for (var at = Placed(folder); at is { } id && visited.Add(id); at = Placed(_folders[id].ParentId))
        {
            names.Add(_folders[id].Name);
        }

        names.Reverse();
        return names;
    }

    /// <summary>The folder path of the entity <paramref name="id"/> - the card header's breadcrumb; empty for the root or an unknown entity.</summary>
    public IReadOnlyList<string> EntityPath(EntityId id) =>
        _entities.TryGetValue(id, out var node) ? FolderPath(node.Entity.FolderId) : [];

    private FolderId? Placed(FolderId? folder) =>
        folder is { } id && _folders.ContainsKey(id) ? id : null;

    private FolderId Key(FolderId? folder) => Placed(folder) ?? default;

    private static EntityNode BuildNode(CampaignEntity entity, EntityResolver resolver)
    {
        var resolved = resolver.Resolve(entity);
        var name = entity.Label ?? resolved.Source?.Entry.Name ?? entity.Source.Entry.ToString();

        return new EntityNode(entity, name, resolved);
    }
}
