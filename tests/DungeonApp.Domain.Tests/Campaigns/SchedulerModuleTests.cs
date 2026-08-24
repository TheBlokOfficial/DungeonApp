using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules.Clock;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;

namespace DungeonApp.Domain.Tests.Campaigns;

public sealed class SchedulerModuleTests
{
    [Fact]
    public void Scheduled_event_becomes_due_only_when_clock_crosses_its_due_time()
    {
        var campaign = new Campaign(CampaignId.New(), "Ashen Vale", [new ClockModule(), new SchedulerModule()]);

        campaign.Execute(new ScheduleWorldEvent(
            Guid.NewGuid(),
            TimeSpan.FromHours(2),
            "Caravan arrives",
            "The merchants reach the south gate."));
        var beforeDue = campaign.Execute(new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.FromHours(1), "Travel continues."));
        var whenDue = campaign.Execute(new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.FromHours(1), "Travel continues."));

        Assert.Single(beforeDue);
        Assert.IsType<WorldTimeAdvanced>(beforeDue[0].Payload);
        Assert.Collection(
            whenDue,
            worldTimeAdvanced => Assert.IsType<WorldTimeAdvanced>(worldTimeAdvanced.Payload),
            due =>
            {
                var dueEvent = Assert.IsType<ScheduledWorldEventDue>(due.Payload);
                Assert.Equal("Caravan arrives", dueEvent.ScheduledEvent.Title);
                Assert.Equal(TimeSpan.FromHours(2), due.OccurredAt.Elapsed);
            });
        Assert.Empty(campaign.GetModule<SchedulerModule>(SchedulerModule.Id).ScheduledEvents);
        Assert.Equal([1L, 2L, 3L, 4L], campaign.History.Select(campaignEvent => campaignEvent.SequenceNumber));
    }

    [Fact]
    public void Scheduler_requires_the_clock_module()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Campaign(CampaignId.New(), "Missing dependency", [new SchedulerModule()]));
    }

    [Fact]
    public void Enabling_scheduler_without_clock_is_rejected_without_configuration_history()
    {
        var campaign = new Campaign(
            CampaignId.New(),
            "Clockless campaign",
            moduleFactories: [new SchedulerModuleFactory()]);

        Assert.Throws<InvalidOperationException>(
            () => campaign.Execute(new EnableCampaignModule(Guid.NewGuid(), SchedulerModule.Id, "Track upcoming events.")));

        Assert.Empty(campaign.History);
    }
}
