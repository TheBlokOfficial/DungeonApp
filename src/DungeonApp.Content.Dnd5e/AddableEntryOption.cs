using DungeonApp.Core.Content;

namespace DungeonApp.Content.Dnd5e;

/// <summary>
/// One entry the campaign tool's "add" picker offers: resolved, and belonging to this content set
/// (<see cref="Dnd5eContentSet.Id"/>) - the only two things <see cref="CampaignInstancesToolViewModel"/>
/// checks before listing an entry as something the GM can bring into the world.
/// <para>
/// <see cref="ToString"/> is what the picker's default item display reads, since the view carries no
/// item template of its own for this option.
/// </para>
/// </summary>
public sealed record AddableEntryOption(string Name, EntryAddress Address)
{
    public override string ToString() => Name;
}
