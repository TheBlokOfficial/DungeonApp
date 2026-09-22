using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// The campaign the GM currently has open, and the one way anything changes it.
/// <para>
/// The Core deliberately has no notion of a current campaign, so this is where that lives.
/// <see cref="ChangeAsync"/> is the single door every module panel's write goes through: it applies
/// exactly the <see cref="CampaignChange"/> it was handed to the campaign's state, saves the whole
/// result in one commit, and only then raises <see cref="Changed"/> - carrying nothing but a
/// read-only snapshot, so a subscriber has no way to turn around and mutate anything from inside
/// the notification (docs/architecture.md, "Gdzie mieszka stan").
/// </para>
/// </summary>
public sealed class CampaignSession(
    Campaign campaign,
    ICampaignRepository repository,
    IReadOnlyList<StateModelDeclaration> declarations)
{
    private bool _changing;
    private bool _notifying;

    /// <summary>
    /// Raised after a change has been committed - or after an attempt to commit it failed, see
    /// <see cref="CampaignChangeResult.SaveWarning"/>. Carries the new snapshot; nothing here can be
    /// used to write, and calling <see cref="ChangeAsync"/> from inside a handler is refused (see
    /// <see cref="CampaignChangeDenial.DuringNotification"/>) rather than merely discouraged.
    /// </summary>
    public event Action<CampaignStateSnapshot>? Changed;

    public Campaign Campaign { get; private set; } = campaign;

    /// <summary>
    /// Applies <paramref name="change"/>, saves the whole campaign in one commit, and raises
    /// <see cref="Changed"/> - in that order, always. Refuses instead of queueing when another
    /// change is still running (including its own save) or when called from inside the notification
    /// a previous change's commit is still raising: both leave the campaign exactly as it was.
    /// <para>
    /// A refused call and a call whose save failed are told apart in the result: a refusal changes
    /// nothing, while a failed save still applies the change in memory and still notifies - the GM
    /// has to know the table is not on disk, which <see cref="CampaignChangeResult.SaveWarning"/> is
    /// for.
    /// </para>
    /// </summary>
    public async Task<CampaignChangeResult> ChangeAsync(CampaignChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (_notifying)
        {
            return CampaignChangeResult.Denied(CampaignChangeDenial.DuringNotification);
        }

        if (_changing)
        {
            return CampaignChangeResult.Denied(CampaignChangeDenial.ChangeInProgress);
        }

        _changing = true;
        CampaignChangeResult result;

        try
        {
            var updated = Campaign.WithSnapshot(Campaign.Snapshot.Apply(change));

            try
            {
                await repository.SaveAsync(updated, declarations);
                result = CampaignChangeResult.Applied;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The change stands in memory and the next successful save will carry it, so this is
                // a warning rather than a rollback - but the GM has to know the table is not on disk.
                result = CampaignChangeResult.SaveFailed(
                    "Zmiana nie została zapisana na dysku. Sprawdź dostęp do katalogu kampanii.");
            }

            Campaign = updated;
        }
        finally
        {
            _changing = false;
        }

        _notifying = true;
        List<Exception>? failures = null;

        try
        {
            // Each subscriber is invoked on its own, never through the multicast delegate's own
            // Invoke: one throwing must not stop the rest from seeing the snapshot, and the change is
            // already committed regardless of what a view does with the notification.
            foreach (var subscriber in Changed?.GetInvocationList() ?? [])
            {
                try
                {
                    ((Action<CampaignStateSnapshot>)subscriber)(Campaign.Snapshot);
                }
                catch (Exception ex)
                {
                    (failures ??= []).Add(ex);
                }
            }
        }
        finally
        {
            _notifying = false;
        }

        if (failures is [var only])
        {
            ExceptionDispatchInfo.Capture(only).Throw();
        }
        else if (failures is { Count: > 1 })
        {
            throw new AggregateException(failures);
        }

        return result;
    }
}
