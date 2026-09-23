using DungeonApp.Core.State;

namespace DungeonApp.Library.Entries.Instances;

/// <summary>
/// The state model instances live in - the first one any system declares, and the example the
/// declaration shape (<see cref="StateModelDeclaration{TRecord}"/>) was designed against. Owned by
/// the entries library, hence the <c>entries.</c> prefix on its id - given before the model moved
/// out of <c>DungeonApp.Core</c>, so the id on disk did not change with the move.
/// </summary>
public static class InstancesModel
{
    public static readonly StateModelDeclaration<CampaignInstance> Declaration = new("entries.instances", 1);
}
