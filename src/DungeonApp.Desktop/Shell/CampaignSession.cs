using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// The campaign the GM currently has open, and the one way anything changes it.
/// <para>
/// The Core deliberately has no notion of a current campaign, so this is where that lives. Every
/// module panel runs its operations through <see cref="ExecuteAsync"/> rather than reaching for the
/// repository, which keeps the three steps that must always happen together in one place: run the
/// operation, keep the refusal readable, write the result down.
/// </para>
/// </summary>
public sealed class CampaignSession(
    Campaign campaign,
    ICampaignRepository repository,
    ICampaignJournalStore journal)
{
    /// <summary>
    /// Raised after an operation has been committed. Panels that show something derived from the
    /// campaign - a pending list, the chronicle - reload on this rather than each subscribing to
    /// every module's announcements and still missing the GM's own corrections.
    /// </summary>
    public event Action? Committed;

    public Campaign Campaign { get; } = campaign;

    /// <summary>
    /// Runs one operation and saves what it changed. Returns null when it went through, or the
    /// sentence to show the GM when it did not.
    /// <para>
    /// Saving after every operation rather than on a timer: the campaign is a local single-user
    /// document, a save costs milliseconds, and an unsaved table is the one failure the GM cannot
    /// recover from. A refused operation changed nothing, so it is not written at all.
    /// </para>
    /// </summary>
    public async Task<string?> ExecuteAsync(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            operation();
        }
        catch (CampaignRuleException refusal)
        {
            // The rules said no. Nothing changed, and the module already phrased why.
            return refusal.Message;
        }

        try
        {
            await repository.SaveAsync(Campaign);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The change stands in memory and the next successful save will carry it, so this is a
            // warning rather than a rollback - but the GM has to know the table is not on disk.
            Committed?.Invoke();

            return "Zmiana nie została zapisana na dysku. Sprawdź dostęp do katalogu kampanii.";
        }

        Committed?.Invoke();

        return null;
    }

    /// <summary>
    /// The tail of the chronicle, most recent first. Read from disk rather than from memory: the
    /// campaign holds only what has not been written yet.
    /// </summary>
    public async Task<IReadOnlyList<JournalEntry>> ReadChronicleAsync(int limit)
    {
        try
        {
            return await journal.ReadRecentAsync(Campaign.Id, limit);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A chronicle that cannot be read is a loss, never a failure: it holds no state.
            return [];
        }
    }
}
