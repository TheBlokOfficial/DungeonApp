using DungeonApp.Core.State;

namespace DungeonApp.Library.Entries.Instances;

/// <summary>
/// The state model instances live in - the first one any system declares, and the example the
/// declaration shape (<see cref="StateModelDeclaration{TRecord}"/>) was designed against. Owned by
/// the entry/pack/registry mechanism that is itself moving to the library
/// (docs/code-map.md, "Instancje w kampanii": the move out of <c>DungeonApp.Core</c> is a later
/// étape), hence the <c>entries.</c> prefix on its id today, ahead of the move itself.
/// </summary>
public static class InstancesModel
{
    public static readonly StateModelDeclaration<CampaignInstance> Declaration = new("entries.instances", 1);
}
