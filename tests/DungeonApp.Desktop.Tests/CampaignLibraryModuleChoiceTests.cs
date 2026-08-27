using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// What the GM is offered when starting a campaign, and what actually ends up in it. The rules
/// belong to the catalogue; what is checked here is that the screen never offers a set the Core
/// would refuse, and never quietly creates a campaign different from the one on screen.
/// </summary>
public sealed class CampaignLibraryModuleChoiceTests
{
    private readonly RecordingRepository _repository = new();
    private readonly ModuleCatalog _catalog = new ModuleCatalog()
        .Register(ClockModule.Id, () => new ClockModule())
        .Register(SchedulerModule.Id, () => new SchedulerModule())
        .Register(PartyModule.Id, () => new PartyModule());

    private readonly CampaignLibraryViewModel _library;

    public CampaignLibraryModuleChoiceTests()
        => _library = new CampaignLibraryViewModel(
            _repository,
            new CreateCampaign(_repository, _catalog, TimeProvider.System),
            _catalog,
            _ => Task.CompletedTask);

    private ModuleChoiceViewModel Choice(ModuleId id) =>
        _library.ModuleChoices.First(choice => choice.Id == id);

    [Fact]
    public void Offers_EverythingTheBuildHas_AllSwitchedOn()
    {
        Assert.Equal(
            [ClockModule.Id, SchedulerModule.Id, PartyModule.Id],
            _library.ModuleChoices.Select(choice => choice.Id));
        Assert.All(_library.ModuleChoices, choice => Assert.True(choice.IsChosen));
    }

    [Fact]
    public void SwitchingOffAModuleAnotherNeeds_HoldsItAndSaysWhy()
    {
        Choice(ClockModule.Id).IsChosen = false;

        var clock = Choice(ClockModule.Id);
        Assert.True(clock.IsChosen);
        Assert.False(clock.CanChange);
        Assert.Contains("Harmonogram", clock.RequiredBy);
    }

    [Fact]
    public void SwitchingOffTheModuleThatNeededIt_ReleasesTheHold()
    {
        Choice(SchedulerModule.Id).IsChosen = false;

        var clock = Choice(ClockModule.Id);
        Assert.True(clock.CanChange);
        Assert.Null(clock.RequiredBy);

        clock.IsChosen = false;
        Assert.False(clock.IsChosen);
    }

    /// <summary>A module with nothing depending on it is the GM's to refuse.</summary>
    [Fact]
    public void SwitchingOffAModuleNothingNeeds_LeavesItOff()
    {
        Choice(PartyModule.Id).IsChosen = false;

        Assert.False(Choice(PartyModule.Id).IsChosen);
    }

    [Fact]
    public async Task Creating_GivesTheCampaignExactlyWhatIsOnScreen()
    {
        Choice(SchedulerModule.Id).IsChosen = false;
        Choice(ClockModule.Id).IsChosen = false;
        _library.NewCampaignName = "Kroniki Doliny";

        await _library.CreateCommand.ExecuteAsync();

        var saved = Assert.Single(_repository.Saved);
        Assert.Equal([PartyModule.Id], saved.Modules.Active.Select(module => module.Manifest.Id));
    }

    private sealed class RecordingRepository : ICampaignRepository
    {
        public List<Campaign> Saved { get; } = [];

        public Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
        {
            Saved.Add(campaign);
            return Task.CompletedTask;
        }

        public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved.FirstOrDefault(campaign => campaign.Id == id));

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>(
                [.. Saved.Select(campaign => new CampaignSummary(campaign.Id, campaign.Name, campaign.CreatedAt))]);
    }
}
