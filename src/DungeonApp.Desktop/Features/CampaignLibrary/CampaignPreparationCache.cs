using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Core.Systems;
using DungeonApp.Desktop.Content;

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
/// Every method here is keyed by a <see cref="CampaignSummary"/>, not a bare <see cref="CampaignId"/>:
/// opening a campaign now means resolving <em>which</em> system's declarations - and which of
/// <paramref name="repositoriesBySystem"/> - to read it with. <see cref="CampaignSummary.DirectorySystemId"/>
/// answers both: it names the compiled system whose own directory this summary was listed from (always
/// one of <paramref name="systems"/>, never a stranger - a repository is never built for an uncompiled
/// system in the first place). A summary whose manifest (<see cref="CampaignSummary.SystemId"/>) names a
/// different system than that is refused before any disk read is attempted - see <see cref="Resolve"/>.
/// </para>
/// </summary>
public sealed class CampaignPreparationCache(
    IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem, IReadOnlyList<IGameSystem> systems)
{
    private readonly object _gate = new();
    private readonly Dictionary<CampaignId, Task<Campaign>> _preparations = [];

    public async Task WarmAsync(
        IEnumerable<CampaignSummary> summaries,
        CancellationToken cancellationToken = default)
    {
        using var concurrency = new SemaphoreSlim(initialCount: 2);

        var tasks = summaries
            .Select(summary => WarmBoundedAsync(summary, concurrency, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task<Campaign> TakeAsync(
        CampaignSummary summary,
        CancellationToken cancellationToken = default)
    {
        var task = GetOrCreate(summary, cancellationToken);

        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            RemoveIfCurrent(summary.Id, task);
        }
    }

    /// <summary>
    /// Borrows a prepared campaign for warmup without consuming the cache entry that the user's
    /// first open will need. A broken candidate simply declines warmup; it has already been removed
    /// by the shelf-wide warmup observer.
    /// </summary>
    public async Task<Campaign?> PeekAsync(
        CampaignSummary summary,
        CancellationToken cancellationToken = default)
    {
        var task = GetOrCreate(summary, cancellationToken);

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is CampaignStoreException or CampaignUnavailableException
                                  or IOException or UnauthorizedAccessException)
        {
            RemoveIfCurrent(summary.Id, task);
            return null;
        }
    }

    /// <summary>
    /// The shelf's single source of truth for whether a listed campaign can be opened - the exact same
    /// task <see cref="TakeAsync"/> would await, interpreted into a reason instead of rethrown or
    /// swallowed. Never a second, lighter validator: a campaign whose system is compiled is only ever
    /// called "available" here because this actually read it.
    /// </summary>
    public async Task<CampaignAvailability> CheckAvailabilityAsync(
        CampaignSummary summary,
        CancellationToken cancellationToken = default)
    {
        var task = GetOrCreate(summary, cancellationToken);

        try
        {
            await task.ConfigureAwait(false);
            return CampaignAvailability.Available;
        }
        catch (CampaignUnavailableException ex) when (ex.Reason == CampaignUnavailableReason.MismatchedSystem)
        {
            RemoveIfCurrent(summary.Id, task);
            return CampaignAvailability.MismatchedSystem;
        }
        catch (CampaignStoreException ex) when (ex.Failure == CampaignStoreFailure.UnsupportedFormatVersion)
        {
            RemoveIfCurrent(summary.Id, task);
            return CampaignAvailability.NewerFormat;
        }
        catch (CampaignStoreException ex) when (ex.Failure == CampaignStoreFailure.ModelVersionMismatch)
        {
            RemoveIfCurrent(summary.Id, task);
            return CampaignAvailability.IncompatibleModelVersion;
        }
        catch (Exception ex) when (ex is CampaignStoreException or CampaignUnavailableException
                                  or IOException or UnauthorizedAccessException)
        {
            RemoveIfCurrent(summary.Id, task);
            return CampaignAvailability.Corrupted;
        }
    }

    private Task<Campaign> GetOrCreate(
        CampaignSummary summary,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_preparations.TryGetValue(summary.Id, out var preparation))
            {
                preparation = PrepareAsync(summary, cancellationToken);
                _preparations.Add(summary.Id, preparation);
            }

            return preparation;
        }
    }

    private async Task<Campaign> PrepareAsync(
        CampaignSummary summary,
        CancellationToken cancellationToken)
    {
        var (repository, declarations) = Resolve(summary);

        // JsonCampaignRepository performs asynchronous IO, but also restores module state between
        // awaits. Starting the complete operation on the pool keeps that CPU and first-use JIT cost
        // out of Avalonia's dispatcher.
        return await Task.Run(
                () => repository.GetAsync(summary.Id, declarations, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CampaignUnavailableException(summary.Id, CampaignUnavailableReason.Missing);
    }

    /// <summary>
    /// The one place that decides which repository and which system's declarations a specific campaign
    /// reads with - docs/architecture.md, "Kampania należy do jednego systemu". A manifest-level failure
    /// (<see cref="CampaignSummary.ManifestFailure"/>) is not resolved here at all: an empty declaration
    /// list is handed to the directory's own repository anyway, so its own manifest validation
    /// reproduces the very same <see cref="CampaignStoreException"/> the listing already saw, rather
    /// than this method guessing at or duplicating that reason. The one check nothing else can make -
    /// whether the manifest's own claimed system (if it makes one at all) agrees with the directory this
    /// summary actually came from - happens here, before either repository or declarations are touched.
    /// </summary>
    private (ICampaignRepository Repository, IReadOnlyList<StateModelDeclaration> Declarations) Resolve(
        CampaignSummary summary)
    {
        var repository = repositoriesBySystem[summary.DirectorySystemId];

        if (summary.ManifestFailure is not null)
        {
            return (repository, []);
        }

        if (summary.SystemId is { } manifestSystemId && manifestSystemId != summary.DirectorySystemId)
        {
            throw new CampaignUnavailableException(
                summary.Id, CampaignUnavailableReason.MismatchedSystem, manifestSystemId, summary.DirectorySystemId);
        }

        // DirectorySystemId names a compiled system by construction - only ever set from listing a
        // compiled system's own repository (see CampaignSummary's remarks) - so this lookup cannot miss.
        var system = systems.First(candidate => candidate.Id == summary.DirectorySystemId);

        return (repository, system.StateModels);
    }

    private async Task WarmBoundedAsync(
        CampaignSummary summary,
        SemaphoreSlim concurrency,
        CancellationToken cancellationToken)
    {
        await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await ObserveWarmupAsync(summary, GetOrCreate(summary, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            concurrency.Release();
        }
    }

    private async Task ObserveWarmupAsync(
        CampaignSummary summary,
        Task<Campaign> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is CampaignStoreException or CampaignUnavailableException
                                  or IOException or UnauthorizedAccessException)
        {
            // One damaged or unavailable campaign must not keep the whole application in its startup
            // gate. Opening that row retries and reports the repository's normal error.
            RemoveIfCurrent(summary.Id, task);
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

/// <summary>Why <see cref="CampaignPreparationCache"/> refused a campaign before ever asking the repository to read it.</summary>
public enum CampaignUnavailableReason
{
    /// <summary>The repository answered null: the campaign no longer exists on disk.</summary>
    Missing,

    /// <summary>The campaign's manifest names a system other than the one whose directory it was found in.</summary>
    MismatchedSystem
}

public sealed class CampaignUnavailableException(
    CampaignId id, CampaignUnavailableReason reason, SystemId? manifestSystemId = null, SystemId? directorySystemId = null)
    : Exception(BuildMessage(id, reason, manifestSystemId, directorySystemId))
{
    public CampaignId CampaignId { get; } = id;

    public CampaignUnavailableReason Reason { get; } = reason;

    /// <summary>Set only for <see cref="CampaignUnavailableReason.MismatchedSystem"/>: the system the manifest names.</summary>
    public SystemId? ManifestSystemId { get; } = manifestSystemId;

    /// <summary>Set only for <see cref="CampaignUnavailableReason.MismatchedSystem"/>: the system whose directory this campaign lives in.</summary>
    public SystemId? DirectorySystemId { get; } = directorySystemId;

    private static string BuildMessage(
        CampaignId id, CampaignUnavailableReason reason, SystemId? manifestSystemId, SystemId? directorySystemId) => reason switch
    {
        CampaignUnavailableReason.MismatchedSystem =>
            $"Campaign '{id}' is recorded for system '{manifestSystemId}' but lives in system '{directorySystemId}''s directory.",
        _ => $"Campaign '{id}' no longer exists."
    };
}
