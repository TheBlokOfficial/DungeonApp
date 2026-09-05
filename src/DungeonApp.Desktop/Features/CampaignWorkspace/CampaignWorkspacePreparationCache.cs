using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Desktop.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Desktop.Features.CampaignWorkspace;

/// <summary>
/// Warms every campaign listed on the shelf and hands the prepared state to the first opener. Tasks
/// themselves are cached, so a click that races a refresh awaits the existing read instead of
/// starting a duplicate one.
/// </summary>
public sealed class CampaignWorkspacePreparationCache(
    ICampaignRepository campaigns,
    WorkspaceLayoutStore layouts)
{
    private readonly object _gate = new();
    private readonly Dictionary<CampaignId, Task<CampaignWorkspacePreparation>> _preparations = [];

    public async Task WarmAsync(
        IEnumerable<CampaignSummary> summaries,
        CancellationToken cancellationToken = default)
    {
        using var concurrency = new SemaphoreSlim(initialCount: 2);

        var tasks = summaries
            .Select(summary => WarmBoundedAsync(summary.Id, concurrency, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task<CampaignWorkspacePreparation> TakeAsync(
        CampaignId id,
        CancellationToken cancellationToken = default)
    {
        var task = GetOrCreate(id, cancellationToken);

        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            RemoveIfCurrent(id, task);
        }
    }

    /// <summary>
    /// Borrows prepared state for startup materialization without consuming the cache entry that
    /// the user's first open will need. A broken candidate simply declines visual warmup; it has
    /// already been removed by the shelf-wide warmup observer.
    /// </summary>
    public async Task<CampaignWorkspacePreparation?> PeekAsync(
        CampaignId id,
        CancellationToken cancellationToken = default)
    {
        var task = GetOrCreate(id, cancellationToken);

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is CampaignStoreException or CampaignUnavailableException
                                  or System.IO.IOException or UnauthorizedAccessException)
        {
            RemoveIfCurrent(id, task);
            return null;
        }
    }

    public Task RefreshAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        Task<CampaignWorkspacePreparation> task;

        lock (_gate)
        {
            task = PrepareAsync(id, cancellationToken);
            _preparations[id] = task;
        }

        return ObserveWarmupAsync(id, task);
    }

    private Task<CampaignWorkspacePreparation> GetOrCreate(
        CampaignId id,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_preparations.TryGetValue(id, out var preparation))
            {
                preparation = PrepareAsync(id, cancellationToken);
                _preparations.Add(id, preparation);
            }

            return preparation;
        }
    }

    private async Task<CampaignWorkspacePreparation> PrepareAsync(
        CampaignId id,
        CancellationToken cancellationToken)
    {
        // JsonCampaignRepository performs asynchronous IO, but also restores module state between
        // awaits. Starting the complete operation on the pool keeps that CPU and first-use JIT cost
        // out of Avalonia's dispatcher.
        var campaign = await Task.Run(
                () => campaigns.GetAsync(id, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CampaignUnavailableException(id);

        var layout = await layouts.LoadAsync(id.ToString(), cancellationToken).ConfigureAwait(false);

        return new CampaignWorkspacePreparation(campaign, layout);
    }

    private async Task WarmBoundedAsync(
        CampaignId id,
        SemaphoreSlim concurrency,
        CancellationToken cancellationToken)
    {
        await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await ObserveWarmupAsync(id, GetOrCreate(id, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            concurrency.Release();
        }
    }

    private async Task ObserveWarmupAsync(
        CampaignId id,
        Task<CampaignWorkspacePreparation> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is CampaignStoreException or CampaignUnavailableException
                                  or System.IO.IOException or UnauthorizedAccessException)
        {
            // One damaged or externally removed campaign must not keep the whole application in its
            // startup gate. Opening that row retries and reports the repository's normal error.
            RemoveIfCurrent(id, task);
        }
    }

    private void RemoveIfCurrent(CampaignId id, Task<CampaignWorkspacePreparation> task)
    {
        lock (_gate)
        {
            if (_preparations.TryGetValue(id, out var current) && ReferenceEquals(current, task))
            {
                _preparations.Remove(id);
            }
        }
    }
}

public sealed class CampaignUnavailableException(CampaignId id)
    : Exception($"Campaign '{id}' no longer exists.")
{
    public CampaignId CampaignId { get; } = id;
}
