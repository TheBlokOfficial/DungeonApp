namespace DungeonApp.Core.State;

/// <summary>
/// Marks a type as one model's record kind - state the frame can store, version, and hand back
/// through a <see cref="CampaignStateSnapshot"/>. Carries only the record's own identity, nothing
/// more: no base class, no behaviour, so a system's record stays a plain value the deserializer
/// alone validates (docs/architecture.md, "Gdzie mieszka stan": "Zdolność do zapisu jest
/// oznaczeniem typu, nie wspólnym przodkiem z logiką").
/// </summary>
public interface IStateRecord
{
    /// <summary>This record's identity within its model - stable across every edit that keeps it
    /// the same thing, the way <c>CampaignInstance</c>'s own <c>InstanceId</c> never changes across
    /// a relabel or a patch replacement.</summary>
    string Id { get; }
}
