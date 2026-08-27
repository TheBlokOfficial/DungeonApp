using System;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// The roster taken through a save and a reopen. Who sits at the table is the one thing a GM would
/// never forgive the application for losing.
/// </summary>
public sealed class PartyRoundTripTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly JsonCampaignJournalStore _journal;
    private readonly JsonCampaignRepository _repository;

    public PartyRoundTripTests()
    {
        var catalog = new ModuleCatalog().Register(PartyModule.Id, () => new PartyModule());

        _journal = new JsonCampaignJournalStore(_library.Path);
        _repository = new JsonCampaignRepository(
            _library.Path, catalog, _journal, new FixedTimeProvider(Moment));
    }

    public void Dispose() => _library.Dispose();

    [Fact]
    public async Task Remembers_who_plays_and_who_turned_up()
    {
        var campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [new PartyModule()], new FixedTimeProvider(Moment));

        var party = campaign.Modules.Get<PartyModule>();
        party.Seat("Ala", "Mirena");
        var bartek = party.Seat("Bartek", "Halgar");
        party.SetPresence(bartek, isPresent: false);

        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);
        var reopenedParty = reopened!.Modules.Get<PartyModule>();

        Assert.Equal(["Ala", "Bartek"], reopenedParty.Participants.Select(p => p.PlayerName));
        Assert.Equal(["Mirena"], reopenedParty.Present.Select(p => p.CharacterName));

        // Identity survives, so a desk panel holding an identifier still points at the same seat.
        Assert.Equal(bartek, reopenedParty.Participants[1].Id);
    }
}
