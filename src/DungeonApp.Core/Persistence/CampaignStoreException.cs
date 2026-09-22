using System;

namespace DungeonApp.Core.Persistence;

/// <summary>Why a stored campaign could not be turned back into a <c>Campaign</c>.</summary>
public enum CampaignStoreFailure
{
    /// <summary>The file could not be read or is not valid JSON.</summary>
    Unreadable,

    /// <summary>Written by a newer build. Refused whole rather than read in part.</summary>
    UnsupportedFormatVersion,

    /// <summary>
    /// Written by an older build, before the campaign document's format last changed shape. Refused
    /// whole rather than migrated - docs/architecture.md, "Wersjonowanie": "Migracji nie budujemy".
    /// </summary>
    LegacyFormatVersion,

    /// <summary>Readable JSON that does not describe a campaign - a missing name, an empty id.</summary>
    Invalid,

    /// <summary>
    /// The manifest and a model's state file disagree: a save was interrupted between them. Better
    /// reported than half loaded in silence.
    /// </summary>
    TornSave,

    /// <summary>
    /// A model's state file was written at a version this build's declaration does not declare -
    /// docs/architecture.md, "Wersjonowanie": a mismatch is reported, never guessed at.
    /// </summary>
    ModelVersionMismatch
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
