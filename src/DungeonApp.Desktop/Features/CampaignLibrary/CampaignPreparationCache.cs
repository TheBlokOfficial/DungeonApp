using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;

namespace DungeonApp.Desktop.Features.CampaignLibrary;

/// <summary>
/// Warms every campaign listed on the shelf and hands the loaded campaign to the first opener. Tasks
/// themselves are cached, so a click that races a refresh awaits the existing read instead of
/// starting a duplicate one.
/// <para>
/// Caches only the <see cref="Campaign"/> itself - docs/tasks.md's "Etap 1" moved the desk-layout
/// half of what this cache used to prepare into the desk itself (its own <c>CampaignDesk</c> entry point),
/// which now loads its own layout when a desk tab is actually built.
/// </para>
/// <para>
/// <paramref name="declarations"/> is fixed for the life of this cache, built at the composition
/// root before any system is chosen - see <c>App.axaml.cs</c> for which declarations that actually
/// is today, and its remarks for the limit that choice carries.
/// </para>
/// </summary>
public sealed class CampaignPreparationCache(ICampaignRepository campaigns, IReadOnlyList<StateModelDeclaration> declarations)
{
    private readonly object _gate = new();
    private readonly Dictionary<CampaignId, Task<Campaign>> _preparations = [];

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

    public async Task<Campaign> TakeAsync(
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
    /// Borrows a prepared campaign for warmup without consuming the cache entry that the user's
    /// first open will need. A broken candidate simply declines warmup; it has already been removed
    /// by the shelf-wide warmup observer.
    /// </summary>
    public async Task<Campaign?> PeekAsync(
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

    private Task<Campaign> GetOrCreate(
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

    private async Task<Campaign> PrepareAsync(
        CampaignId id,
        CancellationToken cancellationToken)
    {
        // JsonCampaignRepository performs asynchronous IO, but also restores module state between
        // awaits. Starting the complete operation on the pool keeps that CPU and first-use JIT cost
        // out of Avalonia's dispatcher.
        return await Task.Run(
                () => campaigns.GetAsync(id, declarations, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CampaignUnavailableException(id);
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
        Task<Campaign> task)
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

    private void RemoveIfCurrent(CampaignId id, Task<Campaign> task)
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
