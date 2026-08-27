using System;

namespace DungeonApp.Core.Campaigns;

/// <summary>
/// What the library screen needs to draw a row. Deliberately not a <see cref="Campaign"/>: listing a
/// shelf of campaigns must never mean loading the full state of every one of them.
/// </summary>
public sealed record CampaignSummary(CampaignId Id, CampaignName Name, DateTimeOffset CreatedAt);
