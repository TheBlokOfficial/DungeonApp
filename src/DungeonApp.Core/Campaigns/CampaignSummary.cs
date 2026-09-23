using System;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Systems;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// What the library screen needs to draw a row. Deliberately not a <see cref="Campaign"/>: listing a
/// shelf of campaigns must never mean loading the full state of every one of them.
/// <para>
/// Produced leniently - <see cref="ICampaignRepository.ListAsync"/> never skips a campaign directory,
/// however badly its manifest is damaged, so the shelf can show every one of them, available or not.
/// <see cref="Name"/> and <see cref="CreatedAt"/> fall back to the directory's own name and creation
/// time when the manifest cannot supply them. <see cref="SystemId"/> is null for a campaign whose
/// manifest names no system at all - a pre-system manifest (any format version below the current one)
/// as much as a current one that simply never recorded it - docs/architecture.md, "Kampania należy do
/// jednego systemu". <see cref="ManifestFailure"/> is set only when the manifest itself could not be
/// read at all, or was written by a newer build than this one understands; every other kind of refusal
/// (an incompatible model, a torn save, an unreadable state file) only surfaces once something actually
/// attempts the full read a matched, compiled system's declarations would need.
/// </para>
/// </summary>
public sealed record CampaignSummary(
    CampaignId Id,
    CampaignName Name,
    DateTimeOffset CreatedAt,
    SystemId? SystemId,
    CampaignStoreFailure? ManifestFailure = null);
