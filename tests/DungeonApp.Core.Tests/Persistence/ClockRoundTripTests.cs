using System;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// The first module taken through the whole contract at once: created, run, saved, reopened. If the
/// module design is wrong anywhere, it shows here rather than in a unit test of one part.
/// </summary>
public sealed class ClockRoundTripTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly ModuleCatalog _catalog = new();
    private readonly JsonCampaignJournalStore _journal;
    private readonly JsonCampaignRepository _repository;

    public ClockRoundTripTests()
    {
        _catalog.Register(ClockModule.Id, () => new ClockModule());
        _journal = new JsonCampaignJournalStore(_library.Path);
        _repository = new JsonCampaignRepository(
            _library.Path, _catalog, _journal, new FixedTimeProvider(Moment));
    }

    public void Dispose() => _library.Dispose();

    private async Task<Campaign> NewSavedCampaign()
    {
        var campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [new ClockModule()], new FixedTimeProvider(Moment));

        await _repository.SaveAsync(campaign);

        return campaign;
    }

    [Fact]
    public async Task Carries_world_time_across_a_reopen()
    {
        var campaign = await NewSavedCampaign();
        campaign.Modules.Get<ClockModule>().Advance(TimeSpan.FromHours(30), "podróż przez przełęcz");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);

        Assert.Equal(TimeSpan.FromHours(30), reopened!.Modules.Get<ClockModule>().Now.Elapsed);
    }

    [Fact]
    public async Task Keeps_the_chronicle_of_what_moved_the_world()
    {
        var campaign = await NewSavedCampaign();
        campaign.Modules.Get<ClockModule>().Advance(TimeSpan.FromHours(8), "nocny odpoczynek");
        await _repository.SaveAsync(campaign);

        var entry = Assert.Single(await _journal.ReadRecentAsync(campaign.Id, 10));

        Assert.Equal(ClockModule.Id, entry.Module);
        Assert.Equal("nocny odpoczynek", entry.Reason);
    }

    /// <summary>A reopened campaign is a working campaign, not a read-only snapshot.</summary>
    [Fact]
    public async Task Keeps_running_from_where_it_left_off()
    {
        var campaign = await NewSavedCampaign();
        campaign.Modules.Get<ClockModule>().Advance(TimeSpan.FromHours(2), "pierwsza sesja");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);
        reopened!.Modules.Get<ClockModule>().Advance(TimeSpan.FromHours(3), "druga sesja");
        await _repository.SaveAsync(reopened);

        var final = await _repository.GetAsync(campaign.Id);

        Assert.Equal(TimeSpan.FromHours(5), final!.Modules.Get<ClockModule>().Now.Elapsed);
        Assert.Equal(2, (await _journal.ReadRecentAsync(campaign.Id, 10)).Count);
    }
}
