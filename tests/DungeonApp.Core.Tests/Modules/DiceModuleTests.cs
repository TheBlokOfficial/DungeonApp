using System;
using System.Linq;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules.Dice;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Modules;

public sealed class DiceModuleTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    /// <summary>A fixed seed, so a test asserts on results rather than on ranges.</summary>
    private readonly DiceModule _dice = new(new Random(1));
    private readonly Campaign _campaign;

    public DiceModuleTests()
        => _campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [_dice], new FixedTimeProvider(Moment));

    [Fact]
    public void Rolls_one_die_and_reports_it_whole()
    {
        var roll = _dice.Roll("k20", "test siły strażnika");

        var die = Assert.Single(roll.Dice);
        Assert.InRange(die, 1, 20);
        Assert.Equal(die, roll.Total);
    }

    [Fact]
    public void Rolls_every_die_it_was_asked_for()
    {
        var roll = _dice.Roll("4k6", "wartości postaci");

        Assert.Equal(4, roll.Dice.Count);
        Assert.All(roll.Dice, die => Assert.InRange(die, 1, 6));
    }

    [Fact]
    public void Adds_the_modifier_once_and_not_to_every_die()
    {
        var roll = _dice.Roll("3k6+2", "atak");

        Assert.Equal(roll.Dice.Sum() + 2, roll.Total);
    }

    [Fact]
    public void Subtracts_a_negative_modifier()
    {
        var roll = _dice.Roll("2k6-1", "osłabienie");

        Assert.Equal(roll.Dice.Sum() - 1, roll.Total);
    }

    /// <summary>The same seed must produce the same session, or nothing here can be checked twice.</summary>
    [Fact]
    public void Rolls_the_same_sequence_from_the_same_seed()
    {
        var other = new DiceModule(new Random(1));
        Campaign.Create(CampaignName.Create("Druga"), [other], new FixedTimeProvider(Moment));

        Assert.Equal(
            _dice.Roll("4k6", "porównanie").Dice,
            other.Roll("4k6", "porównanie").Dice);
    }

    [Fact]
    public void Refuses_what_is_not_a_roll()
        => Assert.Throws<CampaignRuleException>(() => _dice.Roll("dwadzieścia", "cokolwiek"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_roll_with_no_stated_question(string reason)
        => Assert.Throws<CampaignRuleException>(() => _dice.Roll("k20", reason));

    [Fact]
    public void Leaves_the_chronicle_untouched_when_it_refuses()
    {
        Assert.Throws<CampaignRuleException>(() => _dice.Roll("k20", " "));
        Assert.Throws<CampaignRuleException>(() => _dice.Roll("nonsens", "powód"));

        Assert.Empty(_campaign.Journal.Pending);
    }

    /// <summary>
    /// The arithmetic, not the verdict. A result nobody can take apart is a result the GM has to
    /// trust rather than read.
    /// </summary>
    [Fact]
    public void Writes_down_the_whole_sum_and_why_it_was_rolled()
    {
        var roll = _dice.Roll("2k6+3", "  przekupienie strażnika  ");

        var entry = Assert.Single(_campaign.Journal.Pending);
        Assert.Equal(DiceModule.Id, entry.Module);
        Assert.Equal(
            $"Rzut 2k6+3: {roll.Dice[0]} + {roll.Dice[1]} + 3 = {roll.Total}",
            entry.Summary);
        Assert.Equal("przekupienie strażnika", entry.Reason);
    }

    /// <summary>A lone die needs no arithmetic shown - the total is the die.</summary>
    [Fact]
    public void Writes_a_single_die_without_showing_a_sum()
    {
        var roll = _dice.Roll("k20", "inicjatywa");

        Assert.Equal($"Rzut k20: {roll.Total}", Assert.Single(_campaign.Journal.Pending).Summary);
    }

    [Fact]
    public void Refuses_to_work_before_its_campaign_activates_it()
        => Assert.Throws<InvalidOperationException>(() => new DiceModule().Roll("k20", "próba"));
}
