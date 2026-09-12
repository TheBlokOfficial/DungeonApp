using System;

namespace DungeonApp.Core.Persistence;

/// <summary>Why a stored campaign could not be turned back into a <c>Campaign</c>.</summary>
public enum CampaignStoreFailure
{
    /// <summary>The file could not be read or is not valid JSON.</summary>
    Unreadable,

    /// <summary>Written by a newer build. Refused whole rather than read in part.</summary>
    UnsupportedFormatVersion,

    /// <summary>Readable JSON that does not describe a campaign - a missing name, an empty id.</summary>
    Invalid,

    /// <summary>
    /// The manifest and the data block value files disagree: a save was interrupted between them. Better
    /// reported than half loaded in silence.
    /// </summary>
    TornSave
}

/// <summary>
/// Raised instead of returning a half-built campaign. The message is diagnostic; wording shown to
/// the GM is the UI's job, which is what <see cref="Failure"/> is for.
/// </summary>
public sealed class CampaignStoreException(CampaignStoreFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CampaignStoreFailure Failure { get; } = failure;
}
