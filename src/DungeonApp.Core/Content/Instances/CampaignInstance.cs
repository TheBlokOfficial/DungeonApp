using DungeonApp.Core.State;

namespace DungeonApp.Core.Content.Instances;

/// <summary>
/// One in-campaign occurrence of an <see cref="Entry"/> - the "instancja" of
/// docs/architecture.md's "Wpis, dokument, instancja, nakładka" section. The entry is the essence;
/// this is a particular specimen of it, standing somewhere in somebody's campaign.
/// <para>
/// A state record (<see cref="IStateRecord"/>): the frame stores, versions, and hands this type
/// back through a <see cref="CampaignStateSnapshot"/> without ever knowing what it means. Named
/// properties with <see langword="required"/> rather than a positional record, the same way every
/// deserializer-validated content record in this application is - the deserializer is the only
/// validator this record gets.
/// </para>
/// <para>
/// <see cref="Patch"/> is sparse, and the instance is a link to <see cref="Source"/> rather than a
/// copy of it. That is the whole point: an edit to the entry is treated the way a balance patch to
/// a game is treated, so a corrected value shipped in a pack has to reach the campaigns that
/// already use it. Materializing the entry's values into the instance at creation time would sever
/// exactly that. Reading an instance is therefore the entry's values with
/// <see cref="ContentValues.Overlay"/> applied; saving one is
/// <see cref="ContentValues.Difference"/> against them.
/// </para>
/// <para>
/// <see cref="Label"/> is the GM's own name for this specimen - "Goblin 2". It is a property of the
/// instance in the engine and deliberately not a field of the content, which is what lets the
/// engine name a specimen while knowing nothing whatsoever about what kind of thing it is.
/// <see langword="null"/> means the GM has not named this one.
/// </para>
/// </summary>
public sealed record CampaignInstance : IStateRecord
{
    public required InstanceId Id { get; init; }

    public required EntryAddress Source { get; init; }

    public string? Label { get; init; }

    public required ContentValues Patch { get; init; }

    /// <summary>The frame's own view of this record's identity - see <see cref="IStateRecord"/>. Never the public <see cref="Id"/> itself, which callers reach for by its real type instead.</summary>
    string IStateRecord.Id => Id.Value.ToString();
}
