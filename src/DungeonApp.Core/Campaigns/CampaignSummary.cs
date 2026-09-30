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
/// time when the manifest cannot supply them. <see cref="ManifestFailure"/> is set only when the
/// manifest itself could not be read at all, or was written by a newer build than this one
/// understands; every other kind of refusal (an incompatible model, a torn save, an unreadable state
/// file) only surfaces once something actually attempts the full read a matched, compiled system's
/// declarations would need.
/// </para>
/// <para>
/// A campaign belongs to one system - the one in whose directory it lies.
/// <see cref="DirectorySystemId"/> is that system - always known, since a repository is
/// only ever built for one compiled system's own campaign directory (never for an unknown one).
/// <see cref="SystemId"/> is the separate, nullable claim the manifest itself makes: null for a
/// pre-system manifest (any format version below the current one) as much as a current one that simply
/// never recorded one. The two agreeing is what "available" means; a manifest naming a different system
/// than the directory it was found in is the mismatch <c>CampaignPreparationCache</c> refuses before
/// ever reading further.
/// </para>
/// </summary>
public sealed record CampaignSummary(
    CampaignId Id,
    CampaignName Name,
    DateTimeOffset CreatedAt,
    SystemId DirectorySystemId,
    SystemId? SystemId,
    CampaignStoreFailure? ManifestFailure = null);
