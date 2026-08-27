using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Modules;

public sealed class SchedulerModuleTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly ClockModule _clock = new();
    private readonly SchedulerModule _scheduler = new();
    private readonly Campaign _campaign;

    public SchedulerModuleTests()
        => _campaign = Campaign.Create(
            CampaignName.Create("Kroniki Doliny"), [_scheduler, _clock], new FixedTimeProvider(Moment));

    /// <summary>
    /// The dependency is what the whole declare-rather-than-discover rule exists for: a scheduler
    /// with no clock is refused when the campaign is assembled, not when time first moves.
    /// </summary>
    [Fact]
    public void Cannot_be_switched_on_without_a_clock()
    {
        var exception = Assert.Throws<ModuleActivationException>(() => Campaign.Create(
            CampaignName.Create("Bez zegara"), [new SchedulerModule()], new FixedTimeProvider(Moment)));

        Assert.Equal(ModuleActivationFailure.MissingDependency, exception.Failure);
    }

    [Fact]
    public void Keeps_what_is_not_due_yet()
    {
        _scheduler.Schedule(TimeSpan.FromHours(8), "przybycie kupca", "umowa z karczmarzem");

        _clock.Advance(TimeSpan.FromHours(2), "podróż");

        Assert.Single(_scheduler.Pending);
    }

    [Fact]
    public void Announces_what_has_come_due()
    {
        var heard = new List<ScheduledWorldEventDue>();
        _campaign.Events.Subscribe<ScheduledWorldEventDue>(heard.Add);

        var scheduled = _scheduler.Schedule(TimeSpan.FromHours(2), "przybycie kupca", "umowa");

        _clock.Advance(TimeSpan.FromHours(3), "podróż");

        var announcement = Assert.Single(heard);

        Assert.Equal(scheduled, announcement.Id);
        Assert.Equal("przybycie kupca", announcement.Name);
        Assert.Equal(TimeSpan.FromHours(2), announcement.DueAt.Elapsed);
        Assert.Empty(_scheduler.Pending);
    }

    /// <summary>
    /// One move of the clock can bring several things due at once, and they must resolve in due
    /// order every time - the same save has to play out the same way tomorrow.
    /// </summary>
    [Fact]
    public void Resolves_several_due_events_in_world_time_order()
    {
        _scheduler.Schedule(TimeSpan.FromHours(5), "zmierzch", "pora dnia");
        _scheduler.Schedule(TimeSpan.FromHours(1), "posiłek", "pora dnia");
        _scheduler.Schedule(TimeSpan.FromHours(3), "zmiana warty", "rozkaz");

        _clock.Advance(TimeSpan.FromHours(6), "cały dzień marszu");

        var names = _campaign.Journal.Pending
            .Where(entry => entry.Summary.StartsWith("Nadszedł termin", StringComparison.Ordinal))
            .Select(entry => entry.Summary)
            .ToArray();

        Assert.Equal(3, names.Length);
        Assert.Contains("posiłek", names[0], StringComparison.Ordinal);
        Assert.Contains("zmiana warty", names[1], StringComparison.Ordinal);
        Assert.Contains("zmierzch", names[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Fires_an_event_due_exactly_now()
    {
        _scheduler.Schedule(TimeSpan.FromHours(2), "przybycie kupca", "umowa");

        _clock.Advance(TimeSpan.FromHours(2), "podróż");

        Assert.Empty(_scheduler.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Refuses_to_schedule_anything_but_the_future(int hours)
        => Assert.Throws<CampaignRuleException>(
            () => _scheduler.Schedule(TimeSpan.FromHours(hours), "cokolwiek", "próba"));

    [Theory]
    [InlineData("", "powód")]
    [InlineData("   ", "powód")]
    [InlineData("nazwa", "")]
    [InlineData("nazwa", "  ")]
    public void Refuses_a_plan_missing_its_name_or_its_reason(string name, string reason)
        => Assert.Throws<CampaignRuleException>(
            () => _scheduler.Schedule(TimeSpan.FromHours(1), name, reason));

    [Fact]
    public void Leaves_the_calendar_untouched_when_it_refuses()
    {
        _scheduler.Schedule(TimeSpan.FromHours(2), "przybycie kupca", "umowa");

        Assert.Throws<CampaignRuleException>(() => _scheduler.Schedule(TimeSpan.FromHours(-1), "wstecz", "próba"));

        Assert.Single(_scheduler.Pending);
    }

    [Fact]
    public void Writes_down_both_the_plan_and_its_arrival()
    {
        _scheduler.Schedule(TimeSpan.FromHours(2), "przybycie kupca", "umowa z karczmarzem");
        _clock.Advance(TimeSpan.FromHours(3), "podróż");

        var summaries = _campaign.Journal.Pending.Select(entry => entry.Summary).ToArray();

        Assert.Contains(summaries, summary => summary.Contains("Zaplanowano", StringComparison.Ordinal));
        Assert.Contains(summaries, summary => summary.Contains("Nadszedł termin", StringComparison.Ordinal));
    }

    [Fact]
    public void Survives_being_captured_and_restored()
    {
        _scheduler.Schedule(TimeSpan.FromHours(2), "przybycie kupca", "umowa");

        var restored = new SchedulerModule();
        restored.RestoreState(_scheduler.CaptureState(), _scheduler.Manifest.StateVersion);

        Assert.Equal("przybycie kupca", Assert.Single(restored.Pending).Name);
    }
}
