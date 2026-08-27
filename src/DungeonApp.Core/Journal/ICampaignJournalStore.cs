using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;

namespace DungeonApp.Core.Journal;

/// <summary>
/// Where the chronicle is kept. A separate port from the campaign store because the journal has a
/// different lifecycle and a different criticality: it is append-only, it grows without limit, and
/// losing it must never cost the campaign.
/// </summary>
public interface ICampaignJournalStore
{
    Task AppendAsync(
        CampaignId campaign,
        IReadOnlyList<JournalEntry> entries,
        CancellationToken cancellationToken = default);

    /// <summary>Most recent first, so the caller can show the last few without reading the lot.</summary>
    Task<IReadOnlyList<JournalEntry>> ReadRecentAsync(
        CampaignId campaign,
        int limit,
        CancellationToken cancellationToken = default);
}
