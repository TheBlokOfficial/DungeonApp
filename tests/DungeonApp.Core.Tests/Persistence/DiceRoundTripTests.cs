using System;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Dice;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// A module that keeps nothing, taken through a save and a reopen. The contract has to carry an
/// empty state as calmly as a full one, or every future module would be pushed into inventing state
/// it does not have.
/// </summary>
public sealed class DiceRoundTripTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly JsonCampaignJournalStore _journal;
    private readonly JsonCampaignRepository _repository;

    public DiceRoundTripTests()
    {
        var catalog = new ModuleCatalog().Register(DiceModule.Id, () => new DiceModule());

        _journal = new JsonCampaignJournalStore(_library.Path);
        _repository = new JsonCampaignRepository(
            _library.Path, catalog, _journal, new FixedTimeProvider(Moment));
    }

    public void Dispose() => _library.Dispose();

    [Fact]
    public async Task Reopens_and_still_rolls()
    {
        var campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"),
            [new DiceModule(new Random(1))],
            new FixedTimeProvider(Moment));

        campaign.Modules.Get<DiceModule>().Roll("k20", "test siły strażnika");
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);
        var roll = reopened!.Modules.Get<DiceModule>().Roll("2k6+3", "przekupienie");

        Assert.Equal(2, roll.Dice.Count);

        // The roll survives in the chronicle rather than in the module, which is the whole reason
        // the module has no state to restore.
        var chronicle = await _journal.ReadRecentAsync(campaign.Id, 10);
        Assert.Contains(chronicle, entry => entry.Summary.StartsWith("Rzut k20", StringComparison.Ordinal));
    }
}
