using System;
using System.IO;
using Avalonia.Threading;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

/// <summary>
/// Coalesces layout writes. <see cref="MarkDirty"/> is called once per completed gesture and per
/// open/close/minimize/activate - never per pointer move, which would mean hundreds of synchronous
/// file writes a second.
/// <para>
/// A synchronous save on the UI thread is right here: the file is a few kilobytes and writes are
/// coalesced to at most one per debounce window. Moving it to a background thread would require
/// snapshotting view model state under a lock, which is far more machinery than a millisecond of IO.
/// </para>
/// </summary>
public sealed class WorkspaceLayoutSession
{
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(750);

    private readonly WorkspaceLayoutStore _store;
    private readonly string _workspaceId;
    private readonly Func<WorkspaceLayout> _snapshot;
    private readonly DispatcherTimer _timer;

    private bool _isDirty;

    public WorkspaceLayoutSession(WorkspaceLayoutStore store, string workspaceId, Func<WorkspaceLayout> snapshot)
    {
        _store = store;
        _workspaceId = workspaceId;
        _snapshot = snapshot;

        // On the UI thread deliberately, so the snapshot can read view model state directly.
        _timer = new DispatcherTimer(DebounceInterval, DispatcherPriority.Background, (_, _) => Flush());
    }

    public WorkspaceLayout Load() => _store.Load(_workspaceId);

    public void MarkDirty()
    {
        _isDirty = true;

        // Restarting rather than letting it run gives a trailing debounce, so a burst of gestures
        // collapses into a single write once the user pauses.
        _timer.Stop();
        _timer.Start();
    }

    /// <summary>
    /// Writes immediately if anything is pending. Idempotent, so it is safe to call on shutdown as
    /// well as from the timer. A failed write must not take the application down with it - the user
    /// loses an arrangement, which is not worth a crash on exit.
    /// </summary>
    public void Flush()
    {
        _timer.Stop();

        if (!_isDirty)
        {
            return;
        }

        _isDirty = false;

        try
        {
            _store.Save(_workspaceId, _snapshot());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
