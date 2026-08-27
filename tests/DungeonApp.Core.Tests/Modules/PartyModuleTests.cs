using System;
using System.Linq;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Modules;

public sealed class PartyModuleTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly PartyModule _party = new();
    private readonly Campaign _campaign;

    public PartyModuleTests()
        => _campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [_party], new FixedTimeProvider(Moment));

    [Fact]
    public void Starts_with_nobody_at_the_table()
        => Assert.Empty(_party.Participants);

    [Fact]
    public void Seats_a_player_as_present()
    {
        _party.Seat("Ala", "Mirena");

        var participant = Assert.Single(_party.Participants);
        Assert.Equal("Ala", participant.PlayerName);
        Assert.Equal("Mirena", participant.CharacterName);
        Assert.True(participant.IsPresent);
    }

    [Fact]
    public void Seats_a_player_who_has_no_character_yet()
    {
        _party.Seat("Ala");

        Assert.Null(Assert.Single(_party.Participants).CharacterName);
    }

    [Fact]
    public void Keeps_seating_order()
    {
        _party.Seat("Ala");
        _party.Seat("Bartek");
        _party.Seat("Celina");

        Assert.Equal(["Ala", "Bartek", "Celina"], _party.Participants.Select(p => p.PlayerName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_player_with_no_name(string name)
        => Assert.Throws<CampaignRuleException>(() => _party.Seat(name));

    [Fact]
    public void Refuses_a_name_longer_than_it_will_keep()
        => Assert.Throws<CampaignRuleException>(
            () => _party.Seat(new string('a', PartyModule.MaxNameLength + 1)));

    /// <summary>Two identical names cannot be told apart on the desk or in the chronicle.</summary>
    [Fact]
    public void Refuses_a_player_already_at_the_table()
    {
        _party.Seat("Ala");

        Assert.Throws<CampaignRuleException>(() => _party.Seat("  ala  "));
        Assert.Single(_party.Participants);
    }

    [Fact]
    public void Trims_what_it_is_given()
    {
        _party.Seat("  Ala  ", "  Mirena  ");

        var participant = Assert.Single(_party.Participants);
        Assert.Equal("Ala", participant.PlayerName);
        Assert.Equal("Mirena", participant.CharacterName);
    }

    [Fact]
    public void Marks_somebody_away_and_back()
    {
        var id = _party.Seat("Ala");

        _party.SetPresence(id, isPresent: false);
        Assert.Empty(_party.Present);

        _party.SetPresence(id, isPresent: true);
        Assert.Single(_party.Present);
    }

    /// <summary>The chronicle explains the campaign; it is not a log of every touch of a checkbox.</summary>
    [Fact]
    public void Writes_nothing_when_presence_does_not_change()
    {
        var id = _party.Seat("Ala");
        var recorded = _campaign.Journal.Pending.Count;

        _party.SetPresence(id, isPresent: true);

        Assert.Equal(recorded, _campaign.Journal.Pending.Count);
    }

    [Fact]
    public void Changes_which_character_a_player_leads()
    {
        var id = _party.Seat("Ala", "Mirena");

        _party.SetCharacter(id, "Halgar");

        Assert.Equal("Halgar", Assert.Single(_party.Participants).CharacterName);
    }

    [Fact]
    public void Takes_a_character_away_when_given_nothing()
    {
        var id = _party.Seat("Ala", "Mirena");

        _party.SetCharacter(id, "   ");

        Assert.Null(Assert.Single(_party.Participants).CharacterName);
    }

    [Fact]
    public void Removes_a_seat()
    {
        var id = _party.Seat("Ala");
        _party.Seat("Bartek");

        _party.Remove(id);

        Assert.Equal(["Bartek"], _party.Participants.Select(p => p.PlayerName));
    }

    /// <summary>
    /// A desk panel can be a moment behind the roster. That is something the GM should read, not a
    /// crash, so it is refused the same way any other rule is.
    /// </summary>
    [Fact]
    public void Refuses_to_touch_somebody_who_is_not_there()
    {
        var id = _party.Seat("Ala");
        _party.Remove(id);

        Assert.Throws<CampaignRuleException>(() => _party.SetPresence(id, isPresent: false));
        Assert.Throws<CampaignRuleException>(() => _party.SetCharacter(id, "Mirena"));
        Assert.Throws<CampaignRuleException>(() => _party.Remove(id));
    }

    [Fact]
    public void Leaves_the_roster_untouched_when_it_refuses()
    {
        _party.Seat("Ala", "Mirena");

        Assert.Throws<CampaignRuleException>(() => _party.Seat("Ala"));
        Assert.Throws<CampaignRuleException>(() => _party.Seat(" "));

        var participant = Assert.Single(_party.Participants);
        Assert.Equal("Mirena", participant.CharacterName);
        Assert.Single(_campaign.Journal.Pending);
    }

    /// <summary>
    /// The roster is bookkeeping, not a change to the world, so its entries carry no grounds - but
    /// they still say who wrote them.
    /// </summary>
    [Fact]
    public void Writes_down_who_joined_without_demanding_a_reason()
    {
        _party.Seat("Ala", "Mirena");

        var entry = Assert.Single(_campaign.Journal.Pending);
        Assert.Equal(PartyModule.Id, entry.Module);
        Assert.Equal("Do drużyny dołącza Ala jako Mirena", entry.Summary);
        Assert.Null(entry.Reason);
    }

    [Fact]
    public void Refuses_to_work_before_its_campaign_activates_it()
        => Assert.Throws<InvalidOperationException>(() => new PartyModule().Seat("Ala"));
}
