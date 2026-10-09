using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Core.World;

/// <summary>New entities as one change, plus their ids so the view that asked can select them.</summary>
public sealed record AddedEntities(CampaignChange Change, IReadOnlyList<EntityId> Ids);

/// <summary>A new folder as one change, plus its id.</summary>
public sealed record AddedFolder(CampaignChange Change, FolderId Id);

/// <summary>
/// Builds the <see cref="CampaignChange"/> for each thing a GM does to the world tree: one GM
/// action is one change. Pure, like <see cref="CampaignEntityChanges"/> - they read the snapshot
/// they are given and touch nothing; the caller applies the change through the campaign's single
/// entry point.
/// <para>
/// Each operation that can be refused has a <c>...Problem</c> twin returning the reason in Polish
/// (<see langword="null"/> when allowed), which a view shows next to a disabled menu item; the
/// factory itself throws <see cref="InvalidOperationException"/> with the same text, because
/// reaching it after the check passed is a programming error.
/// </para>
/// </summary>
public static class WorldChanges
{
    /// <summary>
    /// Creates <paramref name="count"/> entities of <paramref name="source"/> in <paramref name="folder"/>
    /// (null is the root), with the next <paramref name="count"/> consecutive numbers; the counter moves
    /// in the same change.
    /// </summary>
    public static AddedEntities AddEntities(
        CampaignStateSnapshot snapshot, EntryAddress source, string? label, int count, FolderId? folder)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        RequireFolder(snapshot, folder);

        var change = new CampaignChange();
        var ids = new List<EntityId>(count);
        var last = WorldNumbering.LastNumber(snapshot);

        for (var i = 0; i < count; i++)
        {
            var entity = new CampaignEntity
            {
                Id = EntityId.New(),
                Source = source,
                Label = CampaignEntityChanges.NormalizeLabel(label),
                FolderId = folder,
                Number = ++last,
                Patch = ContentValues.Empty,
            };

            ids.Add(entity.Id);
            change.Upsert(EntitiesModel.Declaration, entity);
        }

        change.Upsert(WorldModels.Counter, new WorldCounter { LastNumber = last });

        return new AddedEntities(change, ids);
    }

    /// <summary>Why <paramref name="name"/> cannot name a folder, or <see langword="null"/>.</summary>
    public static string? FolderNameProblem(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Nazwa katalogu nie może być pusta." : null;

    /// <summary>A new folder named <paramref name="name"/> (trimmed) inside <paramref name="parent"/>; null is the root.</summary>
    public static AddedFolder CreateFolder(CampaignStateSnapshot snapshot, FolderId? parent, string? name)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        RequireName(name);
        RequireFolder(snapshot, parent);

        var folder = new WorldFolder { Id = FolderId.New(), Name = name!.Trim(), ParentId = parent };

        return new AddedFolder(new CampaignChange().Upsert(WorldModels.Folders, folder), folder.Id);
    }

    /// <summary>Renames a folder; its place and contents stay.</summary>
    public static CampaignChange RenameFolder(CampaignStateSnapshot snapshot, FolderId id, string? name)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        RequireName(name);

        var folder = snapshot.Get(WorldModels.Folders).GetValueOrDefault(id.ToString())
            ?? throw new ArgumentException($"No folder {id}.", nameof(id));

        return new CampaignChange().Upsert(WorldModels.Folders, folder with { Name = name!.Trim() });
    }

    /// <summary>Why this selection cannot go into <paramref name="target"/> (null is the root), or <see langword="null"/>.</summary>
    public static string? MoveProblem(
        CampaignStateSnapshot snapshot, IReadOnlyCollection<FolderId> folders, IReadOnlyCollection<EntityId> entities, FolderId? target)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(entities);

        if (folders.Count == 0 && entities.Count == 0)
        {
            return "Nic nie zaznaczono.";
        }

        var all = snapshot.Get(WorldModels.Folders);

        if (target is { } targetId && !all.ContainsKey(targetId.ToString()))
        {
            return "Nie ma takiego katalogu.";
        }

        // Walk up from the target: meeting a moved folder means the target is that folder or inside it.
        var moved = folders.ToHashSet();
        var visited = new HashSet<FolderId>();

        for (var at = target; at is { } current && visited.Add(current); at = all.GetValueOrDefault(current.ToString())?.ParentId)
        {
            if (moved.Contains(current))
            {
                return "Katalog nie może trafić do samego siebie ani do swojego katalogu.";
            }
        }

        return null;
    }

    /// <summary>
    /// Moves the folders (with everything inside them) and entities to <paramref name="target"/>; null
    /// is the root. Throws when <see cref="MoveProblem"/> would report something.
    /// </summary>
    public static CampaignChange Move(
        CampaignStateSnapshot snapshot, IReadOnlyCollection<FolderId> folders, IReadOnlyCollection<EntityId> entities, FolderId? target)
    {
        if (MoveProblem(snapshot, folders, entities, target) is { } problem)
        {
            throw new InvalidOperationException(problem);
        }

        var change = new CampaignChange();
        var knownFolders = snapshot.Get(WorldModels.Folders);
        var knownEntities = snapshot.Get(EntitiesModel.Declaration);

        var moved = folders.ToHashSet();

        foreach (var id in folders)
        {
            var folder = knownFolders.GetValueOrDefault(id.ToString()) ?? throw new ArgumentException($"No folder {id}.", nameof(folders));

            // A folder inside another moved folder travels with it; moving it on its own would take it out.
            if (!HasMovedAncestor(knownFolders, folder.ParentId, moved))
            {
                change.Upsert(WorldModels.Folders, folder with { ParentId = target });
            }
        }

        foreach (var id in entities)
        {
            var entity = knownEntities.GetValueOrDefault(id.ToString()) ?? throw new ArgumentException($"No entity {id}.", nameof(entities));

            if (!HasMovedAncestor(knownFolders, entity.FolderId, moved))
            {
                change.Upsert(EntitiesModel.Declaration, entity with { FolderId = target });
            }
        }

        return change;
    }

    // Walks up from the folder; a visited set keeps a damaged parent loop from spinning.
    private static bool HasMovedAncestor(IReadOnlyDictionary<string, WorldFolder> known, FolderId? start, HashSet<FolderId> moved)
    {
        var visited = new HashSet<FolderId>();

        for (var at = start; at is { } current && visited.Add(current); at = known.GetValueOrDefault(current.ToString())?.ParentId)
        {
            if (moved.Contains(current))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the entities and the empty folders as one change. Throws when a folder is not empty
    /// (<see cref="DeleteFolderProblem"/>).
    /// </summary>
    public static CampaignChange Delete(
        CampaignStateSnapshot snapshot, IReadOnlyCollection<FolderId> folders, IReadOnlyCollection<EntityId> entities)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(entities);

        var change = new CampaignChange();

        foreach (var id in folders)
        {
            if (DeleteFolderProblem(snapshot, id) is { } problem)
            {
                throw new InvalidOperationException(problem);
            }

            change.Delete(WorldModels.Folders, id.ToString());
        }

        foreach (var id in entities)
        {
            change.Delete(EntitiesModel.Declaration, id.ToString());
        }

        return change;
    }

    /// <summary>Removes the entities. Their numbers stay spent: the counter is not touched.</summary>
    public static CampaignChange DeleteEntities(IReadOnlyCollection<EntityId> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var change = new CampaignChange();

        foreach (var id in entities)
        {
            change.Delete(EntitiesModel.Declaration, id.ToString());
        }

        return change;
    }

    /// <summary>Why this folder cannot be deleted - only an empty one can - or <see langword="null"/>.</summary>
    public static string? DeleteFolderProblem(CampaignStateSnapshot snapshot, FolderId id)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var occupied = snapshot.Get(EntitiesModel.Declaration).Values.Any(entity => entity.FolderId == id)
            || snapshot.Get(WorldModels.Folders).Values.Any(folder => folder.ParentId == id);

        return occupied ? "Katalog nie jest pusty." : null;
    }

    /// <summary>Removes empty folders. Throws when any is not empty (<see cref="DeleteFolderProblem"/>).</summary>
    public static CampaignChange DeleteFolders(CampaignStateSnapshot snapshot, IReadOnlyCollection<FolderId> folders)
    {
        ArgumentNullException.ThrowIfNull(folders);

        var change = new CampaignChange();

        foreach (var id in folders)
        {
            if (DeleteFolderProblem(snapshot, id) is { } problem)
            {
                throw new InvalidOperationException(problem);
            }

            change.Delete(WorldModels.Folders, id.ToString());
        }

        return change;
    }

    private static void RequireName(string? name)
    {
        if (FolderNameProblem(name) is { } problem)
        {
            throw new InvalidOperationException(problem);
        }
    }

    private static void RequireFolder(CampaignStateSnapshot snapshot, FolderId? folder)
    {
        if (folder is { } id && !snapshot.Get(WorldModels.Folders).ContainsKey(id.ToString()))
        {
            throw new ArgumentException($"No folder {id}.", nameof(folder));
        }
    }
}
