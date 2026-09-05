namespace DungeonApp.Core.DataBlocks;

/// <summary>Why a data block that a save mentions could not be turned back into a value.</summary>
public enum DataBlockUnreadableReason
{
    /// <summary>The manifest names a data block this build has never heard of.</summary>
    UnknownToRegistry,

    /// <summary>
    /// The data block is known, but its stored value was written at a shape version this build does
    /// not have a migration for.
    /// </summary>
    UnsupportedVersion,
}

/// <summary>
/// One data block <see cref="CampaignDataBlocks.Hydrate"/> could not turn into a value, together with
/// why. Carries no bytes and no version numbers of its own - those live wherever the value was going
/// to come from (the persistence layer), which is also who is responsible for leaving that source
/// untouched. This record exists only to let a caller enumerate which ids are affected and say why in
/// a sentence a GM can read.
/// </summary>
public sealed record UnreadableDataBlock(DataBlockId Id, DataBlockUnreadableReason Reason);
