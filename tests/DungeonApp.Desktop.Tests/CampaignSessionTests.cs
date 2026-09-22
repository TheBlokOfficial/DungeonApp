using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The single door any change goes through. Exercises the ordering and refusal guarantees
/// docs/architecture.md's "Gdzie mieszka stan" names for it: save before notify, a refusal changes
/// and notifies nothing, a failed save still notifies with a warning, and the fifth ban is
/// structural rather than counted - a change called from inside the notification a previous one is
/// still raising is refused, not merely discouraged.
/// </summary>
public sealed class CampaignSessionTests
{
    private static readonly StateModelDeclaration<FakeRecord> Model = new("test.records", 1);

    private static Campaign NewCampaign() => Campaign.Create(CampaignName.Create("Testowa"), TimeProvider.System);

    private static CampaignChange AddChange(string id) => new CampaignChange().Upsert(Model, new FakeRecord(id));

    [Fact]
    public async Task Save_happens_before_the_change_notification_and_the_notification_sees_the_saved_state()
    {
        var repository = new RecordingRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);
        var order = new List<string>();
        repository.OnSaveStarting = () => order.Add("save-start");
        repository.OnSaveFinished = () => order.Add("save-end");

        string[]? seenIds = null;
        session.Changed += snapshot =>
        {
            order.Add("notified");
            seenIds = [.. snapshot.Get(Model).Keys];
        };

        await session.ChangeAsync(AddChange("a"));

        Assert.Equal(["save-start", "save-end", "notified"], order);
        Assert.Equal(["a"], seenIds!);
    }

    [Fact]
    public async Task A_change_called_from_inside_the_notification_is_refused_and_changes_nothing_further()
    {
        var repository = new RecordingRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);

        CampaignChangeResult? nested = null;
        session.Changed += _ => nested = session.ChangeAsync(AddChange("from-handler")).GetAwaiter().GetResult();

        await session.ChangeAsync(AddChange("a"));

        Assert.Equal(CampaignChangeDenial.DuringNotification, nested!.Denial);
        Assert.DoesNotContain("from-handler", session.Campaign.Snapshot.Get(Model).Keys);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task A_second_call_while_the_first_is_still_saving_is_refused_and_changes_nothing()
    {
        var repository = new GatedRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);

        // ChangeAsync runs synchronously up to repository.SaveAsync's own first await, so by the
        // time this call returns a task, the save has already started and _changing is already set
        // - no extra synchronization is needed to make the race deterministic.
        var first = session.ChangeAsync(AddChange("a"));

        var second = await session.ChangeAsync(AddChange("b"));

        Assert.Equal(CampaignChangeDenial.ChangeInProgress, second.Denial);
        Assert.DoesNotContain("b", session.Campaign.Snapshot.Get(Model).Keys);

        repository.Gate.SetResult(true);
        var firstResult = await first;
        Assert.False(firstResult.WasDenied);
    }

    [Fact]
    public async Task A_failed_save_leaves_the_change_in_the_snapshot_sets_a_warning_and_still_notifies()
    {
        var repository = new RecordingRepository { ThrowOnSave = true };
        var session = new CampaignSession(NewCampaign(), repository, [Model]);
        var notified = false;
        session.Changed += _ => notified = true;

        var result = await session.ChangeAsync(AddChange("a"));

        Assert.False(result.WasDenied);
        Assert.NotNull(result.SaveWarning);
        Assert.True(notified);
        Assert.Contains("a", session.Campaign.Snapshot.Get(Model).Keys);
    }

    /// <summary>
    /// A subscriber throwing must not stop the rest from seeing the snapshot - the change is already
    /// committed by the time notification starts, regardless of what a view does with it. The
    /// exception still has to reach the caller (nothing here may swallow it), and the notification
    /// flag has to come back down so the next change is not permanently refused.
    /// </summary>
    [Fact]
    public async Task A_throwing_subscriber_does_not_stop_the_others_or_lose_the_save_and_still_surfaces()
    {
        var repository = new RecordingRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);

        // Throws only on its first call, so the second ChangeAsync below can prove the flag came
        // back down without itself throwing again and masking that assertion.
        var throwingSubscriberCalls = 0;
        string[]? secondSubscriberSeenIds = null;
        session.Changed += _ =>
        {
            if (++throwingSubscriberCalls == 1)
            {
                throw new InvalidOperationException("first subscriber blew up");
            }
        };
        session.Changed += snapshot => secondSubscriberSeenIds = [.. snapshot.Get(Model).Keys];

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ChangeAsync(AddChange("a")));

        Assert.Equal("first subscriber blew up", exception.Message);
        Assert.Equal(["a"], secondSubscriberSeenIds!);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains("a", session.Campaign.Snapshot.Get(Model).Keys);

        // The notification flag must have come back down despite the throw above.
        var second = await session.ChangeAsync(AddChange("b"));
        Assert.False(second.WasDenied);
        Assert.Equal(2, throwingSubscriberCalls);
    }

    [Fact]
    public async Task A_denial_saves_nothing_and_notifies_nothing()
    {
        var repository = new RecordingRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);
        var notifications = 0;
        session.Changed += _ => notifications++;
        session.Changed += _ => session.ChangeAsync(AddChange("nested")).GetAwaiter().GetResult();

        await session.ChangeAsync(AddChange("a"));

        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task A_change_touching_one_record_leaves_the_others_of_the_same_model_untouched()
    {
        var repository = new RecordingRepository();
        var session = new CampaignSession(NewCampaign(), repository, [Model]);

        await session.ChangeAsync(AddChange("a"));
        await session.ChangeAsync(AddChange("b"));
        await session.ChangeAsync(new CampaignChange().Delete(Model, "a"));

        var remaining = session.Campaign.Snapshot.Get(Model).Keys;
        Assert.DoesNotContain("a", remaining);
        Assert.Contains("b", remaining);
    }

    private sealed record FakeRecord(string Id) : IStateRecord;

    private sealed class RecordingRepository : ICampaignRepository
    {
        public int SaveCount { get; private set; }

        public bool ThrowOnSave { get; init; }

        public Action? OnSaveStarting { get; set; }

        public Action? OnSaveFinished { get; set; }

        public Task SaveAsync(
            Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
        {
            OnSaveStarting?.Invoke();
            SaveCount++;

            if (ThrowOnSave)
            {
                throw new IOException("Simulated disk failure.");
            }

            OnSaveFinished?.Invoke();
            return Task.CompletedTask;
        }

        public Task<Campaign?> GetAsync(
            CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            Task.FromResult<Campaign?>(null);

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }

    /// <summary>A repository whose save does not finish until the test releases <see cref="Gate"/> - the only way to observe the "still saving" half of "trwa inna zmiana" deterministically.</summary>
    private sealed class GatedRepository : ICampaignRepository
    {
        public TaskCompletionSource<bool> Gate { get; } = new();

        public async Task SaveAsync(
            Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            await Gate.Task;

        public Task<Campaign?> GetAsync(
            CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default) =>
            Task.FromResult<Campaign?>(null);

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }
}
