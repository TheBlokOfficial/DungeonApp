using System;
using System.Linq;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Core.World;

/// <summary>
/// The № bookkeeping: the highest number ever handed out, and the one-time numbering of entities that
/// were saved before numbers existed.
/// </summary>
public static class WorldNumbering
{
    /// <summary>
    /// The highest № this campaign has handed out: the counter, or the highest number an entity
    /// holds if that is greater. The next entity gets this plus one. Deleting an entity never
    /// lowers it, because the counter record stays.
    /// </summary>
    public static int LastNumber(CampaignStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var counter = snapshot.Get(WorldModels.Counter).Values.Select(record => record.LastNumber).DefaultIfEmpty(0).Max();
        var held = snapshot.Get(EntitiesModel.Declaration).Values.Select(entity => entity.Number).DefaultIfEmpty(0).Max();

        return Math.Max(counter, held);
    }

    /// <summary>
    /// Numbers entities saved without a № (<c>Number</c> of 0), on read, in the order of their ids,
    /// and moves the counter past them. The order is arbitrary but deterministic, so the same old
    /// file shows the same numbers at every opening; nothing is written here - the numbers become
    /// permanent with the first save, which rewrites every model. Called when a campaign is restored
    /// from disk, so no view ever sees an unnumbered entity.
    /// </summary>
    internal static CampaignStateSnapshot Normalize(CampaignStateSnapshot snapshot)
    {
        var entities = snapshot.Get(EntitiesModel.Declaration);
        var counters = snapshot.Get(WorldModels.Counter);
        var unnumbered = entities.Values.Where(entity => entity.Number <= 0).OrderBy(entity => entity.Id.Value).ToArray();
        var last = LastNumber(snapshot);
        var counterIsBehind = counters.Values.All(record => record.LastNumber < last)
            && (entities.Count > 0 || counters.Count > 0);

        if (unnumbered.Length == 0 && !counterIsBehind)
        {
            return snapshot;
        }

        var change = new CampaignChange();

        foreach (var entity in unnumbered)
        {
            change.Upsert(EntitiesModel.Declaration, entity with { Number = ++last });
        }

        return snapshot.Apply(change.Upsert(WorldModels.Counter, new WorldCounter { LastNumber = last }));
    }
}
