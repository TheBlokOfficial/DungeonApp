using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Events;
using DungeonApp.Domain.Campaigns.Modules;
using DungeonApp.Domain.Campaigns.Modules.Clock;

namespace DungeonApp.Domain.Tests.Campaigns;

public sealed class CampaignClockModuleTests
{
    [Fact]
    public void AdvanceWorldTime_updates_clock_and_records_auditable_event()
    {
        var campaign = new Campaign(CampaignId.New(), "Ashen Vale", [new ClockModule()]);
        var commandId = Guid.NewGuid();

        var committedEvents = campaign.Execute(new AdvanceWorldTime(commandId, TimeSpan.FromMinutes(30), "Travel to the old watchtower."));

        var clock = campaign.GetModule<ClockModule>(ClockModule.Id);
        var timeAdvanced = Assert.IsType<WorldTimeAdvanced>(Assert.Single(committedEvents).Payload);

        Assert.Equal(TimeSpan.FromMinutes(30), clock.CurrentTime.Elapsed);
        Assert.Equal(CampaignTime.Start, timeAdvanced.From);
        Assert.Equal(TimeSpan.FromMinutes(30), timeAdvanced.To.Elapsed);
        Assert.Equal("Travel to the old watchtower.", timeAdvanced.Reason);
        Assert.Equal(commandId, committedEvents[0].CorrelationId);
        Assert.Equal(1, committedEvents[0].SequenceNumber);
        Assert.Equal(committedEvents, campaign.History);
    }

    [Fact]
    public void AdvanceWorldTime_without_clock_module_is_rejected_without_history_change()
    {
        var campaign = new Campaign(CampaignId.New(), "Clockless campaign");

        Assert.Throws<CampaignModuleNotEnabledException>(
            () => campaign.Execute(new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.FromMinutes(1), "A minute passes.")));

        Assert.Empty(campaign.History);
    }

    [Fact]
    public void Invalid_clock_command_is_rejected_when_created()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.Zero, "No time passes."));
        Assert.Throws<ArgumentException>(
            () => new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.FromMinutes(1), " "));
    }

    [Fact]
    public void Failed_reaction_does_not_commit_any_module_state_or_history()
    {
        var campaign = new Campaign(CampaignId.New(), "Atomic campaign", [new ClockModule(), new FailingTimeObserverModule()]);

        Assert.Throws<InvalidOperationException>(
            () => campaign.Execute(new AdvanceWorldTime(Guid.NewGuid(), TimeSpan.FromMinutes(30), "Travel.")));

        Assert.Equal(CampaignTime.Start, campaign.GetModule<ClockModule>(ClockModule.Id).CurrentTime);
        Assert.Empty(campaign.History);
    }

    [Fact]
    public void Enable_clock_adds_the_module_and_records_a_configuration_event()
    {
        var campaign = new Campaign(
            CampaignId.New(),
            "Configurable campaign",
            moduleFactories: [new ClockModuleFactory()]);
        var commandId = Guid.NewGuid();

        var committedEvents = campaign.Execute(
            new EnableCampaignModule(commandId, ClockModule.Id, "The campaign now tracks travel time."));

        var configurationEvent = Assert.IsType<CampaignModuleEnabled>(Assert.Single(committedEvents).Payload);

        Assert.Equal(Campaign.CoreModuleId, committedEvents[0].SourceModule);
        Assert.Equal(commandId, committedEvents[0].CorrelationId);
        Assert.Equal(ClockModule.Id, configurationEvent.ModuleId);
        Assert.Equal(1, configurationEvent.StateVersion);
        Assert.Equal(CampaignTime.Start, campaign.GetModule<ClockModule>(ClockModule.Id).CurrentTime);
    }

    [Fact]
    public void Enabling_an_existing_module_is_rejected_without_new_history()
    {
        var campaign = new Campaign(
            CampaignId.New(),
            "Configured campaign",
            [new ClockModule()],
            [new ClockModuleFactory()]);

        Assert.Throws<CampaignModuleAlreadyEnabledException>(
            () => campaign.Execute(new EnableCampaignModule(Guid.NewGuid(), ClockModule.Id, "Duplicate.")));

        Assert.Empty(campaign.History);
    }

    [Fact]
    public void Failed_reaction_to_module_enablement_does_not_change_configuration_or_history()
    {
        var campaign = new Campaign(
            CampaignId.New(),
            "Rejected configuration",
            [new FailingTimeObserverModule()],
            [new ClockModuleFactory()]);

        Assert.Throws<InvalidOperationException>(
            () => campaign.Execute(new EnableCampaignModule(Guid.NewGuid(), ClockModule.Id, "Start tracking time.")));

        Assert.Throws<CampaignModuleNotEnabledException>(() => campaign.GetModule<ClockModule>(ClockModule.Id));
        Assert.Empty(campaign.History);
    }

    private sealed class FailingTimeObserverModule : ICampaignModule
    {
        public CampaignModuleDescriptor Descriptor { get; } = new(new ModuleId("test.failing-time-observer"), 1);

        public ICampaignModule CreateWorkingCopy() => new FailingTimeObserverModule();

        public void Handle(CampaignCommand command, CampaignModuleContext context) => throw new NotSupportedException();

        public void ReactTo(CampaignEvent campaignEvent, CampaignModuleContext context)
        {
            if (campaignEvent.Payload is WorldTimeAdvanced or CampaignModuleEnabled)
            {
                throw new InvalidOperationException("The observer rejected the change.");
            }
        }
    }
}
