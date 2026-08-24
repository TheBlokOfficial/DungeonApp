using DungeonApp.Domain.Campaigns;
using DungeonApp.Domain.Campaigns.Modules.Clock;
using DungeonApp.Domain.Campaigns.Modules.Scheduler;
using DungeonApp.Application.Campaigns;
using DungeonApp.Infrastructure.Campaigns.Persistence;

namespace DungeonApp.Domain.Tests.Campaigns;

public sealed class JsonCampaignRepositoryTests
{
    [Fact]
    public async Task Advance_time_use_case_updates_the_saved_campaign()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "DungeonApp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var repository = new JsonCampaignRepository(directoryPath);
            var campaign = new Campaign(CampaignId.New(), "Ashen Vale", [new ClockModule()]);
            await repository.SaveAsync(campaign);

            var useCase = new AdvanceCampaignTimeUseCase(repository);
            var result = await useCase.ExecuteAsync(
                new AdvanceCampaignTimeRequest(campaign.Id.Value, TimeSpan.FromMinutes(15), "Scouting the road."));

            var reloaded = Assert.IsType<Campaign>(await repository.FindByIdAsync(campaign.Id));

            Assert.Equal(1, Assert.Single(result).SequenceNumber);
            Assert.Equal(TimeSpan.FromMinutes(15), reloaded.GetModule<ClockModule>(ClockModule.Id).CurrentTime.Elapsed);
            Assert.Single(reloaded.History);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Saved_campaign_can_be_listed_opened_and_configured_with_a_clock()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "DungeonApp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var repository = new JsonCampaignRepository(directoryPath);
            var campaign = new Campaign(CampaignId.New(), "Clockless campaign");
            await repository.SaveAsync(campaign);

            var campaigns = await new ListCampaignsUseCase(repository).ExecuteAsync();
            var enabled = await new EnableCampaignModuleUseCase(repository).ExecuteAsync(
                new EnableCampaignModuleRequest(campaign.Id.Value, ClockModule.Id.Value, "Track the expedition."));
            var opened = await new GetCampaignSessionUseCase(repository).ExecuteAsync(campaign.Id.Value);

            var listed = Assert.Single(campaigns);
            Assert.Equal(campaign.Id.Value, listed.Id);
            Assert.Empty(listed.EnabledModuleIds);
            Assert.Contains(ClockModule.Id.Value, enabled.EnabledModuleIds);
            Assert.Equal(TimeSpan.Zero, opened.WorldTime);
            Assert.Contains(opened.History, entry => entry.Title == "Włączono moduł");
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Scheduler_state_survives_json_round_trip_and_due_event_is_persisted()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "DungeonApp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var repository = new JsonCampaignRepository(directoryPath);
            var campaign = new Campaign(CampaignId.New(), "Scheduled campaign", [new ClockModule(), new SchedulerModule()]);
            await repository.SaveAsync(campaign);

            var scheduled = await new ScheduleWorldEventUseCase(repository).ExecuteAsync(
                new ScheduleWorldEventRequest(
                    campaign.Id.Value,
                    TimeSpan.FromMinutes(30),
                    "Guard change",
                    "The north gate changes watch."));
            await new AdvanceCampaignTimeUseCase(repository).ExecuteAsync(
                new AdvanceCampaignTimeRequest(campaign.Id.Value, TimeSpan.FromMinutes(30), "The party waits."));
            var reopened = await new GetCampaignSessionUseCase(repository).ExecuteAsync(campaign.Id.Value);

            Assert.Single(scheduled.ScheduledWorldEvents);
            Assert.Empty(reopened.ScheduledWorldEvents);
            Assert.Contains(reopened.History, entry => entry.Title == "Termin zdarzenia nadszedł");
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Save_and_load_preserves_module_state_and_event_history()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "DungeonApp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var repository = new JsonCampaignRepository(directoryPath);
            var campaign = new Campaign(CampaignId.New(), "Ashen Vale", [new ClockModule()]);
            var firstCommandId = Guid.NewGuid();
            var secondCommandId = Guid.NewGuid();

            campaign.Execute(new AdvanceWorldTime(firstCommandId, TimeSpan.FromMinutes(30), "Travel to the watchtower."));
            campaign.Execute(new AdvanceWorldTime(secondCommandId, TimeSpan.FromHours(2), "A long rest begins."));
            await repository.SaveAsync(campaign);

            var loadedCampaign = await repository.FindByIdAsync(campaign.Id);

            var restored = Assert.IsType<Campaign>(loadedCampaign);
            var restoredClock = restored.GetModule<ClockModule>(ClockModule.Id);

            Assert.Equal(campaign.Id, restored.Id);
            Assert.Equal("Ashen Vale", restored.Name);
            Assert.Equal(TimeSpan.FromMinutes(150), restoredClock.CurrentTime.Elapsed);
            Assert.Collection(
                restored.History,
                first =>
                {
                    Assert.Equal(1, first.SequenceNumber);
                    Assert.Equal(firstCommandId, first.CorrelationId);
                },
                second =>
                {
                    Assert.Equal(2, second.SequenceNumber);
                    Assert.Equal(secondCommandId, second.CorrelationId);
                });
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
