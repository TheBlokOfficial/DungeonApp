using System;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// Two modules that talk to each other, taken through a save and a reopen. Whatever the announcement
/// channel is worth, it is worth nothing if a reopened campaign forgets what it was waiting for.
/// </summary>
public sealed class SchedulerRoundTripTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly ModuleCatalog _catalog = new();
    private readonly JsonCampaignJournalStore _journal;
    private readonly JsonCampaignRepository _repository;

    public SchedulerRoundTripTests()
    {
        _catalog
            .Register(ClockModule.Id, () => new ClockModule())
            .Register(SchedulerModule.Id, () => new SchedulerModule());

        _journal = new JsonCampaignJournalStore(_library.Path);
        _repository = new JsonCampaignRepository(
            _library.Path, _catalog, _journal, new FixedTimeProvider(Moment));
    }

    public void Dispose() => _library.Dispose();

    private async Task<Campaign> NewSavedCampaign()
    {
        var campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"),
            [new ClockModule(), new SchedulerModule()],
            new FixedTimeProvider(Moment));

        await _repository.SaveAsync(campaign);

        return campaign;
    }

    [Fact]
    public async Task Remembers_what_it_was_waiting_for()
    {
        var campaign = await NewSavedCampaign();
        campaign.Modules.Get<SchedulerModule>().Schedule(TimeSpan.FromHours(8), "przybycie kupca", "umowa");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);

        var pending = Assert.Single(reopened!.Modules.Get<SchedulerModule>().Pending);
        Assert.Equal("przybycie kupca", pending.Name);
    }

    /// <summary>
    /// The subscriptions are taken during activation, so a reopened campaign has to be wired up as
    /// well as filled: the scheduler must still hear the clock.
    /// </summary>
    [Fact]
    public async Task Still_reacts_to_the_clock_after_a_reopen()
    {
        var campaign = await NewSavedCampaign();
        campaign.Modules.Get<SchedulerModule>().Schedule(TimeSpan.FromHours(8), "przybycie kupca", "umowa");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);
        reopened!.Modules.Get<ClockModule>().Advance(TimeSpan.FromHours(9), "nocny marsz");
        await _repository.SaveAsync(reopened);

        Assert.Empty(reopened.Modules.Get<SchedulerModule>().Pending);

        var chronicle = await _journal.ReadRecentAsync(campaign.Id, 10);
        Assert.Contains(chronicle, entry => entry.Summary.Contains("Nadszedł termin", StringComparison.Ordinal));
    }

    /// <summary>A campaign that kept the scheduler but lost the clock is not a campaign we can open.</summary>
    [Fact]
    public async Task Refuses_to_open_when_the_dependency_is_gone()
    {
        var campaign = await NewSavedCampaign();

        var withoutTheClock = new JsonCampaignRepository(
            _library.Path,
            new ModuleCatalog().Register(SchedulerModule.Id, () => new SchedulerModule()),
            _journal,
            new FixedTimeProvider(Moment));

        await Assert.ThrowsAsync<CampaignStoreException>(() => withoutTheClock.GetAsync(campaign.Id));
    }
}
