namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// The shelf's one verdict on a listed campaign, computed by <see cref="CampaignPreparationCache.CheckAvailabilityAsync"/>
/// - the same read <see cref="CampaignPreparationCache.TakeAsync"/> performs to actually open one, never a second,
/// lighter validator of its own. Every unavailable value here has exactly one Polish sentence in
/// <c>CampaignRowViewModel</c>, and nothing upstream is meant to distinguish them any further than that sentence does.
/// </summary>
public enum CampaignAvailability
{
    Available,

    /// <summary>The manifest names a system other than the one whose directory this campaign was found in -
    /// including a system this build does not compile at all, which can never equal a directory's own
    /// (always-compiled) system. A manifest that names none at all is not this: it belongs to the directory's
    /// system and opens as Available - docs/architecture.md, "Kampania należy do jednego systemu".</summary>
    MismatchedSystem,

    /// <summary>The manifest was written by a newer build than this one understands.</summary>
    NewerFormat,

    /// <summary>A compiled, matching system exists, but a declared model's stored version does not match what this
    /// build's declaration expects.</summary>
    IncompatibleModelVersion,

    /// <summary>Every other refusal a full read can produce: an unreadable manifest, a torn save, an invalid record.</summary>
    Corrupted
}
