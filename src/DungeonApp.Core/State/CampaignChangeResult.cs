namespace DungeonApp.Core.State;

/// <summary>Why a call to the campaign's single change entry point was refused outright - nothing changed, and there is nothing queued to retry later.</summary>
public enum CampaignChangeDenial
{
    /// <summary>Not a denial - the change was applied.</summary>
    None,

    /// <summary>Another change is still running, including its own save - awaiting it and retrying is the caller's job.</summary>
    ChangeInProgress,

    /// <summary>Called from inside the notification a previous change's commit is still raising.</summary>
    DuringNotification,
}

/// <summary>
/// What came back from one call to the campaign's single change entry point.
/// <para>
/// Keeps today's distinction (docs/tasks.md's brief for this étape) between the two ways a call can
/// fail to leave the GM with nothing to show for it: <see cref="Denial"/> means nothing changed at
/// all, while <see cref="SaveWarning"/> means the change is sitting in memory - and the GM has
/// already been shown it - but did not reach disk.
/// </para>
/// </summary>
public sealed class CampaignChangeResult
{
    private CampaignChangeResult(CampaignChangeDenial denial, string? saveWarning)
    {
        Denial = denial;
        SaveWarning = saveWarning;
    }

    public CampaignChangeDenial Denial { get; }

    public bool WasDenied => Denial != CampaignChangeDenial.None;

    /// <summary>Set only when the change was applied in memory and notified, but writing it to disk failed.</summary>
    public string? SaveWarning { get; }

    /// <summary>The sentence to show the GM, or null when the call went through with nothing to report.</summary>
    public string? Message => Denial switch
    {
        CampaignChangeDenial.None => SaveWarning,
        CampaignChangeDenial.ChangeInProgress =>
            "Trwa już inna zmiana. Poczekaj, aż się zakończy, i spróbuj ponownie.",
        CampaignChangeDenial.DuringNotification =>
            "Nie można rozpocząć zmiany z wnętrza powiadomienia o poprzedniej.",
        _ => null,
    };

    /// <summary>Built by <c>CampaignSession.ChangeAsync</c> - public rather than internal because that type lives in <c>DungeonApp.Desktop</c>, a separate assembly from this one.</summary>
    public static readonly CampaignChangeResult Applied = new(CampaignChangeDenial.None, saveWarning: null);

    public static CampaignChangeResult Denied(CampaignChangeDenial reason) => new(reason, saveWarning: null);

    public static CampaignChangeResult SaveFailed(string warning) => new(CampaignChangeDenial.None, warning);
}
