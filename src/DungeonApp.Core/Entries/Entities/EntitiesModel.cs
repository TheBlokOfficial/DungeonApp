using DungeonApp.Core.State;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// The state model entities live in - the first one any system declares, and the example the
/// declaration shape (<see cref="StateModelDeclaration{TRecord}"/>) was designed against. Owned by
/// the entries library, hence the <c>entries.</c> prefix on its id.
/// </summary>
public static class EntitiesModel
{
    // The id names the file every existing campaign already holds (state/entries.instances.json),
    // so it keeps its original wording rather than following the type's name.
    public static readonly StateModelDeclaration<CampaignEntity> Declaration = new("entries.instances", 1);
}
