namespace DungeonApp.Core.Content;

/// <summary>
/// One in-campaign occurrence of an <see cref="Entry"/> - the "instancja" of
/// docs/architecture.md's "Wpis, dokument, instancja, nakładka" section. The entry is the essence;
/// this is a particular specimen of it, standing somewhere in somebody's campaign.
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
public sealed record CampaignInstance(
    InstanceId Id,
    EntryAddress Source,
    string? Label,
    ContentValues Patch);
