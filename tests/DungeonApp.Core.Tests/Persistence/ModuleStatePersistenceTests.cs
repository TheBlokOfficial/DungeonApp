using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Modules;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

public sealed class ModuleStatePersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly ModuleCatalog _catalog = new();

    public ModuleStatePersistenceTests()
        => _catalog.Register(ModuleId.Create("core.clock"), () => new StubModule("core.clock"));

    public void Dispose() => _library.Dispose();

    private JsonCampaignRepository Repository() => new(_library.Path, _catalog);

    private static Campaign NewCampaign(params ICampaignModule[] modules)
        => Campaign.Create(
            CampaignName.Create("Kroniki Doliny"),
            CampaignModules.Activate(modules),
            new FixedTimeProvider(Moment));

    private string ModuleStatePath(Campaign campaign, string moduleId)
        => Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "modules", $"{moduleId}.json");

    private JsonElement Manifest(Campaign campaign)
        => JsonDocument.Parse(File.ReadAllText(_library.DocumentPath(campaign.Id.Value))).RootElement;

    [Fact]
    public async Task Gives_each_module_its_own_state_file()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));

        await Repository().SaveAsync(campaign);

        Assert.True(File.Exists(ModuleStatePath(campaign, "core.clock")));
        Assert.Equal(
            ["core.clock"],
            Manifest(campaign).GetProperty("activeModules").EnumerateArray().Select(id => id.GetString()));
    }

    [Fact]
    public async Task Reads_module_state_back_into_a_fresh_instance()
    {
        var module = new StubModule("core.clock");
        module.RestoreState(new StubState(42), 1);
        var campaign = NewCampaign(module);

        await Repository().SaveAsync(campaign);
        var reopened = await Repository().GetAsync(campaign.Id);

        var restored = reopened!.Modules.Get<StubModule>();

        Assert.NotSame(module, restored);
        Assert.Equal(42, restored.State.Value);
    }

    /// <summary>
    /// The counter earns its keep here: a state file left behind by an interrupted save disagrees
    /// with the manifest and says so, instead of loading as though it were current.
    /// </summary>
    [Fact]
    public async Task Reports_a_state_file_that_lags_the_manifest()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));
        await Repository().SaveAsync(campaign);

        var statePath = ModuleStatePath(campaign, "core.clock");
        File.WriteAllText(statePath, File.ReadAllText(statePath).Replace("\"generation\": 1", "\"generation\": 0"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => Repository().GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task Reports_a_state_file_that_is_missing_entirely()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));
        await Repository().SaveAsync(campaign);

        File.Delete(ModuleStatePath(campaign, "core.clock"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => Repository().GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    /// <summary>
    /// Opening a campaign without a module it is supposed to be running would quietly show the GM an
    /// incomplete world, so an older build refuses instead - and leaves the state where it is.
    /// </summary>
    [Fact]
    public async Task Refuses_a_campaign_that_needs_a_module_this_build_lacks()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));
        await Repository().SaveAsync(campaign);

        var withoutTheModule = new JsonCampaignRepository(_library.Path, new ModuleCatalog());

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => withoutTheModule.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.UnknownModule, exception.Failure);
        Assert.True(File.Exists(ModuleStatePath(campaign, "core.clock")));
    }

    /// <summary>
    /// Switching a module off and on again must not cost its contents, so a save that no longer runs
    /// it leaves both the entry and the file exactly as they were.
    /// </summary>
    [Fact]
    public async Task Keeps_the_state_of_a_module_that_was_switched_off()
    {
        var module = new StubModule("core.clock");
        module.RestoreState(new StubState(7), 1);

        var withModule = NewCampaign(module);
        await Repository().SaveAsync(withModule);

        var statePath = ModuleStatePath(withModule, "core.clock");
        var beforeDisabling = File.ReadAllText(statePath);

        // The same campaign, saved again with the module switched off.
        var withoutModule = Campaign.Restore(
            withModule.Id, withModule.Name, withModule.CreatedAt, CampaignModules.Activate([]));
        await Repository().SaveAsync(withoutModule);

        Assert.Equal(beforeDisabling, File.ReadAllText(statePath));

        var manifest = Manifest(withModule);
        Assert.Empty(manifest.GetProperty("activeModules").EnumerateArray());

        var retained = Assert.Single(manifest.GetProperty("modules").EnumerateArray());
        Assert.Equal("core.clock", retained.GetProperty("id").GetString());
        Assert.Equal(1, retained.GetProperty("generation").GetInt64());
    }

    /// <summary>Listing draws the shelf from manifests alone, so torn module state cannot hide a campaign.</summary>
    [Fact]
    public async Task Lists_a_campaign_whose_module_state_is_torn()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));
        await Repository().SaveAsync(campaign);

        File.Delete(ModuleStatePath(campaign, "core.clock"));

        var summary = Assert.Single(await Repository().ListAsync());

        Assert.Equal(campaign.Id, summary.Id);
    }

    [Fact]
    public async Task Backs_up_the_module_state_together_with_the_manifest()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));

        await Repository().SaveAsync(campaign);
        await Repository().SaveAsync(campaign);

        var backup = Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "backups", "gen-000001");

        Assert.True(File.Exists(Path.Combine(backup, "campaign.json")));
        Assert.True(File.Exists(Path.Combine(backup, "modules", "core.clock.json")));
    }

    [Fact]
    public async Task Leaves_no_temporary_file_behind()
    {
        var campaign = NewCampaign(new StubModule("core.clock"));

        await Repository().SaveAsync(campaign);
        await Repository().SaveAsync(campaign);

        var directory = _library.CampaignDirectory(campaign.Id.Value);

        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories));
    }
}
