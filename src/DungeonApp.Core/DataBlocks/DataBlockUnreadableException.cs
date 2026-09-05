using System;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// Raised by <see cref="CampaignDataBlocks.Read"/> and <see cref="CampaignDataBlocks.Apply"/> when
/// asked about a data block this campaign could not read back from its save.
/// <para>
/// <see cref="CampaignDataBlocks.Read"/> throws rather than returning null, because null already
/// means "nothing was ever written here" - collapsing the two would make an unreadable block look
/// like an empty one, and the first write through <see cref="CampaignDataBlocks.Apply"/> would then
/// silently overwrite content nobody managed to look at first. Apply throws for the same reason: a
/// value it never read cannot be the basis for a transform that then replaces it.
/// </para>
/// <para>
/// A caller that wants to avoid the exception checks <see cref="CampaignDataBlocks.IsUnreadable"/> or
/// enumerates <see cref="CampaignDataBlocks.UnreadableBlocks"/> first - which is what the UI layer is
/// expected to do to show the GM which blocks survived unread, rather than catching this on the happy
/// path.
/// </para>
/// </summary>
public sealed class DataBlockUnreadableException(DataBlockId id, DataBlockUnreadableReason reason)
    : Exception($"Data block '{id}' could not be read back ({reason}).")
{
    public DataBlockId Id { get; } = id;

    public DataBlockUnreadableReason Reason { get; } = reason;
}
