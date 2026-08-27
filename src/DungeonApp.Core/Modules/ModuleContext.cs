using System;
using DungeonApp.Core.Events;
using DungeonApp.Core.Journal;

namespace DungeonApp.Core.Modules;

/// <summary>
/// The chronicle as one module sees it. Stamping the module and the moment here means a module
/// records what changed and why, and never has to remember to say who it was.
/// </summary>
public sealed class ModuleJournal(CampaignJournal journal, ModuleId module, TimeProvider timeProvider)
{
    /// <summary>
    /// <paramref name="reason"/> is the grounds the change rests on - the GM's stated cause, or the
    /// rule that produced it. Null only where the summary is already the whole story.
    /// </summary>
    public void Record(string summary, string? reason = null) =>
        journal.Record(new JournalEntry(timeProvider.GetUtcNow(), module, summary, reason));
}

/// <summary>
/// What a module is handed when the campaign activates it. One per module, because the journal it
/// carries is stamped with that module's identity.
/// </summary>
public sealed class ModuleContext(CampaignModules modules, ModuleJournal journal, CampaignEvents events)
{
    /// <summary>
    /// The other modules in this campaign, for questions and instructions. Only ones declared in the
    /// manifest may be assumed.
    /// </summary>
    public CampaignModules Modules { get; } = modules;

    public ModuleJournal Journal { get; } = journal;

    /// <summary>
    /// For announcing facts and hearing them. Never for asking another module to do something -
    /// that is what <see cref="Modules"/> is for.
    /// </summary>
    public CampaignEvents Events { get; } = events;
}
