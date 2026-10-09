using DungeonApp.Core.State;
using DungeonApp.Core.World;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// One in-campaign occurrence of an <see cref="Entry"/>. The entry is the essence; this is a
/// particular specimen of it, standing somewhere in somebody's campaign.
/// <para>
/// A state record (<see cref="IStateRecord"/>): the frame stores, versions, and hands this type
/// back through a <see cref="CampaignStateSnapshot"/> without ever knowing what it means. Named
/// properties with <see langword="required"/> rather than a positional record, the same way every
/// deserializer-validated content record in this application is - the deserializer is the only
/// validator this record gets.
/// </para>
/// <para>
/// <see cref="Patch"/> is sparse, and the entity is a link to <see cref="Source"/> rather than a
/// copy of it. That is the whole point: an edit to the entry is treated the way a balance patch to
/// a game is treated, so a corrected value shipped in a pack has to reach the campaigns that
/// already use it. Materializing the entry's values into the entity at creation time would sever
/// exactly that. Reading an entity is therefore the entry's values with
/// <see cref="ContentValues.Overlay"/> applied; saving one is
/// <see cref="ContentValues.Difference"/> against them.
/// </para>
/// <para>
/// <see cref="Label"/> is the GM's own name for this specimen - "Goblin 2". It is a property of the
/// entity in the engine and deliberately not a field of the content, which is what lets the
/// engine name a specimen while knowing nothing whatsoever about what kind of thing it is.
/// <see langword="null"/> means the GM has not named this one.
/// </para>
/// </summary>
public sealed record CampaignEntity : IStateRecord
{
    public required EntityId Id { get; init; }

    public required EntryAddress Source { get; init; }

    public string? Label { get; init; }

    /// <summary>The world folder this entity stands in; <see langword="null"/> is the root. Optional in the file so records saved before folders existed still load.</summary>
    public FolderId? FolderId { get; init; }

    /// <summary>
    /// The entity's № - unique in the campaign, handed out in order when the entity is created and
    /// never changed or reused. 0 only in a record saved before numbers existed;
    /// <see cref="WorldNumbering"/> numbers those on read.
    /// </summary>
    public int Number { get; init; }

    public required ContentValues Patch { get; init; }

    /// <summary>The frame's own view of this record's identity - see <see cref="IStateRecord"/>. Never the public <see cref="Id"/> itself, which callers reach for by its real type instead.</summary>
    string IStateRecord.Id => Id.Value.ToString();
}
