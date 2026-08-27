using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Journal;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

public sealed class CampaignJournalTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly ModuleCatalog _catalog = new();
    private readonly JsonCampaignJournalStore _journal;
    private readonly JsonCampaignRepository _repository;

    public CampaignJournalTests()
    {
        _catalog.Register(ModuleId.Create("core.clock"), () => new StubModule("core.clock"));
        _journal = new JsonCampaignJournalStore(_library.Path);
        _repository = new JsonCampaignRepository(
            _library.Path, _catalog, _journal, new FixedTimeProvider(Moment));
    }

    public void Dispose() => _library.Dispose();

    private Campaign NewCampaign(out StubModule module)
    {
        module = new StubModule("core.clock");

        return Campaign.Create(CampaignName.Create("Kroniki Doliny"), [module], new FixedTimeProvider(Moment));
    }

    private string JournalDirectory(Campaign campaign)
        => Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "journal");

    /// <summary>
    /// Buffered, not appended as it happens: the chronicle and the state it describes reach the disk
    /// together, so it never claims something an unsaved crash rolled back.
    /// </summary>
    [Fact]
    public async Task Holds_entries_until_the_campaign_is_saved()
    {
        var campaign = NewCampaign(out var module);

        module.Set(3, "próba");

        Assert.Single(campaign.Journal.Pending);
        Assert.False(Directory.Exists(JournalDirectory(campaign)));

        await _repository.SaveAsync(campaign);

        Assert.Empty(campaign.Journal.Pending);
        Assert.Single(await _journal.ReadRecentAsync(campaign.Id, 10));
    }

    [Fact]
    public async Task Records_what_changed_who_changed_it_and_why()
    {
        var campaign = NewCampaign(out var module);
        module.Set(3, "gracz przespał noc");

        await _repository.SaveAsync(campaign);

        var entry = Assert.Single(await _journal.ReadRecentAsync(campaign.Id, 10));

        Assert.Equal("Wartość ustawiona na 3", entry.Summary);
        Assert.Equal("gracz przespał noc", entry.Reason);
        Assert.Equal(ModuleId.Create("core.clock"), entry.Module);
        Assert.Equal(Moment, entry.RecordedAt);
    }

    [Fact]
    public async Task Appends_rather_than_replacing_across_saves()
    {
        var campaign = NewCampaign(out var module);

        module.Set(1, "pierwsza");
        await _repository.SaveAsync(campaign);

        module.Set(2, "druga");
        await _repository.SaveAsync(campaign);

        var entries = await _journal.ReadRecentAsync(campaign.Id, 10);

        Assert.Equal(2, entries.Count);
        Assert.Equal(["druga", "pierwsza"], entries.Select(entry => entry.Reason));
    }

    /// <summary>One JSON object per line is what makes appending cheap and a torn line survivable.</summary>
    [Fact]
    public async Task Writes_one_entry_per_line()
    {
        var campaign = NewCampaign(out var module);

        module.Set(1, "pierwsza");
        module.Set(2, "druga");
        await _repository.SaveAsync(campaign);

        var file = Assert.Single(Directory.EnumerateFiles(JournalDirectory(campaign), "*.jsonl"));

        Assert.Equal("2026-08.jsonl", Path.GetFileName(file));
        Assert.Equal(2, File.ReadAllLines(file).Length);
    }

    /// <summary>
    /// The chronicle explains the world but does not hold it, so a line lost to an interrupted write
    /// costs that line and nothing else.
    /// </summary>
    [Fact]
    public async Task Skips_a_damaged_line_and_keeps_the_rest()
    {
        var campaign = NewCampaign(out var module);

        module.Set(1, "pierwsza");
        await _repository.SaveAsync(campaign);

        var file = Directory.EnumerateFiles(JournalDirectory(campaign), "*.jsonl").Single();
        File.AppendAllLines(file, ["{ ucięta linia"]);

        var entry = Assert.Single(await _journal.ReadRecentAsync(campaign.Id, 10));

        Assert.Equal("pierwsza", entry.Reason);
    }

    [Fact]
    public async Task Returns_the_most_recent_entries_first_and_stops_at_the_limit()
    {
        var campaign = NewCampaign(out var module);

        for (var value = 1; value <= 5; value++)
        {
            module.Set(value, $"zmiana {value}");
        }

        await _repository.SaveAsync(campaign);

        var entries = await _journal.ReadRecentAsync(campaign.Id, 2);

        Assert.Equal(["zmiana 5", "zmiana 4"], entries.Select(entry => entry.Reason));
    }

    [Fact]
    public async Task Reports_an_empty_chronicle_for_a_campaign_that_has_written_nothing()
        => Assert.Empty(await _journal.ReadRecentAsync(CampaignId.New(), 10));

    /// <summary>
    /// A reopened campaign holds only what has happened since: the chronicle already on disk is not
    /// pending, and saving again must not write it twice.
    /// </summary>
    [Fact]
    public async Task Does_not_rewrite_the_chronicle_when_a_campaign_is_reopened()
    {
        var campaign = NewCampaign(out var module);
        module.Set(1, "pierwsza");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);
        Assert.Empty(reopened!.Journal.Pending);

        await _repository.SaveAsync(reopened);

        Assert.Single(await _journal.ReadRecentAsync(campaign.Id, 10));
    }
}
