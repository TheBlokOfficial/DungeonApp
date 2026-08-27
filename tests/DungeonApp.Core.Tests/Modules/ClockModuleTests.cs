using System;
using System.Linq;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Modules;

public sealed class ClockModuleTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly ClockModule _clock = new();
    private readonly Campaign _campaign;

    public ClockModuleTests()
        => _campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [_clock], new FixedTimeProvider(Moment));

    [Fact]
    public void Starts_a_campaign_at_zero()
        => Assert.Equal(0, _clock.Now.TotalSeconds);

    [Fact]
    public void Moves_the_world_forward()
    {
        _clock.Advance(TimeSpan.FromHours(3), "gracze przespali noc");

        Assert.Equal(TimeSpan.FromHours(3), _clock.Now.Elapsed);
    }

    [Fact]
    public void Accumulates_across_several_moves()
    {
        _clock.Advance(TimeSpan.FromHours(3), "podróż");
        _clock.Advance(TimeSpan.FromMinutes(30), "postój");

        Assert.Equal(TimeSpan.FromMinutes(210), _clock.Now.Elapsed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Refuses_to_run_backwards_or_stand_still(int hours)
        => Assert.Throws<CampaignRuleException>(
            () => _clock.Advance(TimeSpan.FromHours(hours), "próba"));

    [Fact]
    public void Refuses_an_amount_finer_than_it_can_keep()
        => Assert.Throws<CampaignRuleException>(
            () => _clock.Advance(TimeSpan.FromMilliseconds(500), "próba"));

    /// <summary>The world changes for a reason, and the chronicle has nothing to explain without one.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_move_with_no_stated_reason(string reason)
        => Assert.Throws<CampaignRuleException>(() => _clock.Advance(TimeSpan.FromHours(1), reason));

    /// <summary>
    /// Validate before mutate, in one assertion: a refused command leaves the state untouched and
    /// puts nothing in the chronicle. Everything else in the campaign depends on this holding.
    /// </summary>
    [Fact]
    public void Leaves_the_world_untouched_when_it_refuses()
    {
        _clock.Advance(TimeSpan.FromHours(2), "podróż");

        Assert.Throws<CampaignRuleException>(() => _clock.Advance(TimeSpan.FromHours(-5), "cofnięcie"));
        Assert.Throws<CampaignRuleException>(() => _clock.Advance(TimeSpan.FromHours(1), " "));

        Assert.Equal(TimeSpan.FromHours(2), _clock.Now.Elapsed);
        Assert.Single(_campaign.Journal.Pending);
    }

    [Fact]
    public void Writes_down_what_moved_and_why()
    {
        _clock.Advance(TimeSpan.FromHours(3), "  gracze przespali noc  ");

        var entry = Assert.Single(_campaign.Journal.Pending);

        Assert.Equal(ClockModule.Id, entry.Module);
        Assert.Equal("gracze przespali noc", entry.Reason);
        Assert.Contains("+3 h", entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_the_running_total_in_the_chronicle()
    {
        _clock.Advance(TimeSpan.FromDays(1), "podróż");
        _clock.Advance(TimeSpan.FromMinutes(15), "postój");

        var latest = _campaign.Journal.Pending.Last();

        Assert.Contains("łącznie 1 d 15 min", latest.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Survives_being_captured_and_restored()
    {
        _clock.Advance(TimeSpan.FromHours(5), "podróż");

        var restored = new ClockModule();
        restored.RestoreState(_clock.CaptureState(), _clock.Manifest.StateVersion);

        Assert.Equal(_clock.Now, restored.Now);
    }
}

public sealed class CampaignTimeTests
{
    [Fact]
    public void Cannot_be_negative()
        => Assert.Throws<ArgumentOutOfRangeException>(() => CampaignTime.FromSeconds(-1));

    [Theory]
    [InlineData(0, "0 s")]
    [InlineData(45, "45 s")]
    [InlineData(90, "1 min 30 s")]
    [InlineData(3600, "1 h")]
    [InlineData(93_600, "1 d 2 h")]
    public void Reads_as_its_two_most_significant_units(int seconds, string expected)
        => Assert.Equal(expected, CampaignTime.Describe(TimeSpan.FromSeconds(seconds)));

    /// <summary>Days and minutes with nothing between them must not print an empty hour.</summary>
    [Fact]
    public void Skips_units_that_carry_nothing()
        => Assert.Equal("1 d 15 min", CampaignTime.Describe(TimeSpan.FromMinutes(1455)));
}
