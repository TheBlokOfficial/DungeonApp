using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Desktop.Content;
using DungeonApp.Desktop.Features.CampaignLibrary;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// The shelf's filtering and availability rules - docs/architecture.md, "Kampania należy do jednego
/// systemu": the active system's own campaigns are shown (available or not), a campaign that cannot
/// be assigned to any compiled system is shown everywhere as unavailable, and a campaign belonging to
/// a different, compiled system is hidden. An unavailable row refuses to open and can still be
/// deleted.
/// </summary>
public sealed class CampaignLibraryViewModelTests
{
    private static readonly ContentId SystemA = ContentId.Create("system-a");
    private static readonly ContentId SystemB = ContentId.Create("system-b");
    private static readonly ContentId GhostSystem = ContentId.Create("ghost-system");

    [Fact]
    public async Task Shows_the_active_systems_own_campaign_as_available()
    {
        var summary = MakeSummary("Kroniki", SystemA);
        var repository = new FakeShelfRepository([summary]);
        repository.Campaigns[summary.Id] = MakeCampaign(summary, SystemA);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.True(row.IsAvailable);
        Assert.Null(row.UnavailabilityReason);
    }

    [Fact]
    public async Task Hides_a_campaign_belonging_to_another_compiled_system()
    {
        var summary = MakeSummary("Cudza kampania", SystemB);
        var repository = new FakeShelfRepository([summary]);
        repository.Campaigns[summary.Id] = MakeCampaign(summary, SystemB);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, []), new FakeGameSystem(SystemB, [])], activeSystem: SystemA);
        await library.LoadAsync();

        Assert.Empty(library.Campaigns);
    }

    [Fact]
    public async Task Shows_a_campaign_with_no_system_as_unavailable_everywhere()
    {
        var summary = MakeSummary("Sprzed systemów", systemId: null);
        var repository = new FakeShelfRepository([summary]);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.False(row.IsAvailable);
        Assert.Equal(
            "Kampania bez przypisanego systemu — zapisana starszą wersją programu.",
            row.UnavailabilityReason);
    }

    [Fact]
    public async Task Shows_a_campaign_with_an_uncompiled_system_as_unavailable_everywhere()
    {
        var summary = MakeSummary("Obcy silnik", GhostSystem);
        var repository = new FakeShelfRepository([summary]);

        var library = BuildLibrary(repository, [new FakeGameSystem(SystemA, [])], activeSystem: SystemA);
        await library.LoadAsync();

        var row = Assert.Single(library.Campaigns);
        Assert.False(row.IsAvailable);
        Assert.Contains("ghost-system", row.UnavailabilityReason);
    }

    [Fact]
    public async Task Shows_a_manifest_from_a_newer_build_as_unavailable()
    {
        var summary = MakeSummary("Z przyszłości", systemId: null, manifestFailure: CampaignStoreFailure.UnsupportedFormatVersion);
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
        var summary = MakeSummary("Stary zapis", SystemA);
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
        var summary = MakeSummary("Uszkodzona", SystemA);
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
        var summary = MakeSummary("Bez systemu", systemId: null);
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
        var summary = MakeSummary("Do usunięcia", systemId: null);
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
        ContentId activeSystem,
        Func<CampaignSummary, Task>? onOpen = null)
    {
        var preparations = new CampaignPreparationCache(repository, systems);
        var createCampaign = new CreateCampaign(repository, TimeProvider.System);
        var library = new CampaignLibraryViewModel(
            repository, createCampaign, preparations, systems,
            onOpen ?? (_ => Task.CompletedTask));

        library.SetActiveSystem(systems.Single(system => system.Id == activeSystem));

        return library;
    }

    private static CampaignSummary MakeSummary(
        string name, ContentId? systemId, CampaignStoreFailure? manifestFailure = null) =>
        new(CampaignId.New(), CampaignName.Create(name), DateTimeOffset.UtcNow, systemId, manifestFailure);

    private static Campaign MakeCampaign(CampaignSummary summary, ContentId systemId) =>
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
