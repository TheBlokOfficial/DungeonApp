using System;
using System.Collections.Generic;

namespace DungeonApp.Core.Journal;

/// <summary>
/// Collects chronicle entries until the campaign is saved.
/// <para>
/// Buffered rather than appended as it happens, so the chronicle and the state it describes reach
/// the disk together. Writing each line immediately would leave the journal claiming things that a
/// crash then rolled back out of the unsaved state.
/// </para>
/// </summary>
public sealed class CampaignJournal
{
    private readonly List<JournalEntry> _pending = [];

    public IReadOnlyList<JournalEntry> Pending => _pending;

    public void Record(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _pending.Add(entry);
    }

    /// <summary>Called by the store once the pending entries are safely on disk.</summary>
    public void MarkWritten() => _pending.Clear();
}
