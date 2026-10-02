using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Systems;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The shelf's availability rules - a campaign belongs to exactly one system: the active system's
/// own repository is the only one ever read (there is nothing left to filter - every summary it
/// returns already belongs to that system's directory), a campaign whose manifest names no system
/// belongs to the directory it was found in and opens normally, and a campaign whose manifest
/// names a *different* system than its directory stands on the shelf as unavailable, with a reason. An
/// unavailable row refuses to open and can still be deleted.
/// </summary>
public sealed class CampaignLibraryViewModelTests
{
    private static readonly SystemId SystemA = SystemId.Create("system-a");
    private static readonly SystemId SystemB = SystemId.Create("system-b");

    [Fact]
    public async Task Shows_the_active_systems_own_campaign_as_available()
    {
        var summary = MakeSummary("Kroniki", SystemA, SystemA);
        var repository = new FakeShelfRepository([summary]);
        repository.Campaigns[summary.Id] = MakeCampaign(summary, SystemA);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.True(row.IsAvailable);
        Assert.Null(row.UnavailabilityReason);
    }

    /// <summary>
    /// A pre-system manifest (or a current one that simply never recorded one) belongs to the
    /// directory's own system and opens like any other campaign, never flagged.
    /// </summary>
    [Fact]
    public async Task Shows_a_campaign_with_no_manifest_system_as_belonging_to_the_directorys_system()
    {
        var summary = MakeSummary("Sprzed systemów", SystemA, systemId: null);
        var repository = new FakeShelfRepository([summary]);
        repository.Campaigns[summary.Id] = MakeCampaign(summary, SystemA);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.True(row.IsAvailable);
        Assert.Null(row.UnavailabilityReason);
    }

    /// <summary>
    /// With a per-system directory, this campaign is never even listed by system A's own repository
    /// in the first place, so a manifest naming a different system than the directory it is
    /// actually found in can only mean the directory and the manifest disagree - stood on the shelf
    /// as unavailable, with a reason, never hidden and never opened under either system.
    /// </summary>
    [Fact]
    public async Task Shows_a_campaign_whose_manifest_names_a_different_system_than_its_directory_as_unavailable()
    {
        var summary = MakeSummary("Cudza kampania", directorySystemId: SystemA, systemId: SystemB);
        var repository = new FakeShelfRepository([summary]);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.False(row.IsAvailable);
        Assert.Contains("system-a", row.UnavailabilityReason);
        Assert.Contains("system-b", row.UnavailabilityReason);
    }

    [Fact]
    public async Task Shows_a_manifest_from_a_newer_build_as_unavailable()
    {
        var summary = MakeSummary("Z przyszłości", SystemA, systemId: null, manifestFailure: CampaignStoreFailure.UnsupportedFormatVersion);
        var repository = new FakeShelfRepository([summary]);
        // A real repository's own manifest validation is what actually throws this, given whatever
        // declarations CampaignPreparationCache passes (empty, here, since the manifest itself already
        // failed) - the fake reproduces that coupling rather than guessing at the reason itself.
        repository.Failures[summary.Id] = new CampaignStoreException(CampaignStoreFailure.UnsupportedFormatVersion, "future");

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.Equal("Zapisana nowszą wersją programu.", row.UnavailabilityReason);
    }

    [Fact]
    public async Task Shows_an_incompatible_model_version_as_unavailable()
    {
        var summary = MakeSummary("Stary zapis", SystemA, SystemA);
        var repository = new FakeShelfRepository([summary]);
        repository.Failures[summary.Id] = new CampaignStoreException(CampaignStoreFailure.ModelVersionMismatch, "boom");

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.Equal("Niezgodna wersja danych systemu.", row.UnavailabilityReason);
    }

    [Fact]
    public async Task Shows_a_corrupted_campaign_as_unavailable()
    {
        var summary = MakeSummary("Uszkodzona", SystemA, SystemA);
        var repository = new FakeShelfRepository([summary]);
        repository.Failures[summary.Id] = new CampaignStoreException(CampaignStoreFailure.Unreadable, "boom");

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.Equal("Pliki kampanii są uszkodzone.", row.UnavailabilityReason);
    }

    [Fact]
    public async Task An_unavailable_rows_open_command_refuses_to_run()
    {
        var summary = MakeSummary("Cudza kampania", directorySystemId: SystemA, systemId: SystemB);
        var repository = new FakeShelfRepository([summary]);
        var opened = false;

        var library = BuildLibrary(
            repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA,
            onOpen: _ =>
            {
                opened = true;
                return Task.CompletedTask;
            });
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.False(row.OpenCommand.CanExecute(null));

        await ((DungeonApp.Desktop.ViewModels.AsyncCommand)row.OpenCommand).ExecuteAsync();

        Assert.False(opened);
    }

    [Fact]
    public async Task An_unavailable_campaign_can_still_be_deleted()
    {
        var summary = MakeSummary("Do usunięcia", directorySystemId: SystemA, systemId: SystemB);
        var repository = new FakeShelfRepository([summary]);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        await ((DungeonApp.Desktop.ViewModels.AsyncCommand)row.DeleteCommand).ExecuteAsync();

        Assert.Contains(summary.Id, repository.Deleted);
        Assert.Empty(library.Campaigns);
    }

    private static CampaignLibraryViewModel BuildLibrary(
        FakeShelfRepository repository,
        IReadOnlyList<IGameSystem> systems,
        SystemId activeSystem,
        Func<CampaignSummary, Task>? onOpen = null)
    {
        IReadOnlyDictionary<SystemId, ICampaignRepository> repositoriesBySystem =
            systems.ToDictionary(system => system.Id, _ => (ICampaignRepository)repository);
        var preparations = new CampaignPreparationCache(repositoriesBySystem, systems);
        var createCampaign = new CreateCampaign(repositoriesBySystem, TimeProvider.System);
        var library = new CampaignLibraryViewModel(
            repositoriesBySystem, createCampaign, preparations, systems,
            onOpen ?? (_ => Task.CompletedTask));

        library.SetActiveSystem(systems.Single(system => system.Id == activeSystem));

        return library;
    }

    private static CampaignSummary MakeSummary(
        string name, SystemId directorySystemId, SystemId? systemId, CampaignStoreFailure? manifestFailure = null) =>
        new(CampaignId.New(), CampaignName.Create(name), DateTimeOffset.UtcNow, directorySystemId, systemId, manifestFailure);

    private static Campaign MakeCampaign(CampaignSummary summary, SystemId systemId) =>
        Campaign.Restore(summary.Id, summary.Name, summary.CreatedAt, systemId, CampaignStateSnapshot.Empty);

    private sealed class FakeShelfRepository(IReadOnlyList<CampaignSummary> summaries) : ICampaignRepository
    {
        public Dictionary<CampaignId, Campaign> Campaigns { get; } = [];

        public Dictionary<CampaignId, CampaignStoreException> Failures { get; } = [];

        public List<CampaignId> Deleted { get; } = [];

        public Task SaveAsync(
            Campaign campaign, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
        {
            Campaigns[campaign.Id] = campaign;
            return Task.CompletedTask;
        }

        public Task<Campaign?> GetAsync(
            CampaignId id, IReadOnlyList<StateModelDeclaration> declarations, CancellationToken cancellationToken = default)
        {
            if (Failures.TryGetValue(id, out var failure))
            {
                throw failure;
            }

            return Task.FromResult(Campaigns.GetValueOrDefault(id));
        }

        public Task DeleteAsync(CampaignId id, CancellationToken cancellationToken = default)
        {
            Deleted.Add(id);
            Campaigns.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(summaries);
    }
}
