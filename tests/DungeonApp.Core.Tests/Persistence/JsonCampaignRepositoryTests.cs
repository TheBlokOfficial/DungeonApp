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

public sealed class JsonCampaignRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly ModuleCatalog _catalog = new();
    private readonly JsonCampaignRepository _repository;

    public JsonCampaignRepositoryTests() => _repository = new JsonCampaignRepository(_library.Path, _catalog);

    public void Dispose() => _library.Dispose();

    private static Campaign NewCampaign(string name = "Kroniki Doliny", params ICampaignModule[] modules)
        => Campaign.Create(
            CampaignName.Create(name), CampaignModules.Activate(modules), new FixedTimeProvider(Moment));

    [Fact]
    public async Task Reads_back_everything_it_wrote()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);
        var restored = await _repository.GetAsync(campaign.Id);

        Assert.NotNull(restored);
        Assert.Equal(campaign.Id, restored.Id);
        Assert.Equal(campaign.Name, restored.Name);
        Assert.Equal(campaign.CreatedAt, restored.CreatedAt);
    }

    /// <summary>
    /// The directory is keyed by identity, not by the editable label. This is what will let a rename
    /// stay a rename instead of orphaning the campaign on disk.
    /// </summary>
    [Fact]
    public async Task Keys_the_directory_by_identity()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);

        Assert.True(File.Exists(_library.DocumentPath(campaign.Id.Value)));
    }

    [Fact]
    public async Task Returns_null_for_a_campaign_that_is_not_there()
        => Assert.Null(await _repository.GetAsync(CampaignId.New()));

    [Fact]
    public async Task Reports_an_empty_shelf_before_the_library_exists()
    {
        var repository = new JsonCampaignRepository(Path.Combine(_library.Path, "not-created-yet"), _catalog);

        Assert.Empty(await repository.ListAsync());
    }

    [Fact]
    public async Task Lists_campaigns_in_a_stable_order()
    {
        await _repository.SaveAsync(NewCampaign("Zamek Nocy"));
        await _repository.SaveAsync(NewCampaign("Kroniki Doliny"));
        await _repository.SaveAsync(NewCampaign("Mokradła"));

        var names = (await _repository.ListAsync()).Select(summary => summary.Name.Value).ToArray();

        Assert.Equal(["Kroniki Doliny", "Mokradła", "Zamek Nocy"], names);
    }

    /// <summary>One broken document must not hide every healthy campaign beside it.</summary>
    [Fact]
    public async Task Skips_a_damaged_campaign_when_listing()
    {
        await _repository.SaveAsync(NewCampaign("Kroniki Doliny"));
        _library.WriteDocument(Guid.NewGuid(), "{ this is not json");

        var summaries = await _repository.ListAsync();

        Assert.Equal("Kroniki Doliny", Assert.Single(summaries).Name.Value);
    }

    [Fact]
    public async Task Refuses_a_document_written_by_a_newer_build()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": {{JsonCampaignRepository.CurrentFormatVersion + 1}},
              "id": "{{id}}",
              "name": "Z przyszłości",
              "createdAt": "2026-08-27T18:30:00+00:00"
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id)));

        Assert.Equal(CampaignStoreFailure.UnsupportedFormatVersion, exception.Failure);
    }

    [Fact]
    public async Task Reports_unreadable_json_as_such()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, "{ this is not json");

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id)));

        Assert.Equal(CampaignStoreFailure.Unreadable, exception.Failure);
    }

    [Fact]
    public async Task Rejects_a_document_that_carries_no_usable_name()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": 1,
              "id": "{{id}}",
              "name": "   ",
              "createdAt": "2026-08-27T18:30:00+00:00"
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id)));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    /// <summary>
    /// The reserved fields are a contract, not decoration: they exist so the first ruleset, content
    /// pack and module become entries rather than a reshaping of the manifest.
    /// </summary>
    [Fact]
    public async Task Reserves_room_for_the_ruleset_content_packs_and_modules()
    {
        var campaign = NewCampaign();
        await _repository.SaveAsync(campaign);

        var root = ReadManifest(campaign);

        Assert.Equal(JsonCampaignRepository.CurrentFormatVersion, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("ruleset").ValueKind);
        Assert.Empty(root.GetProperty("contentPacks").EnumerateArray());
        Assert.Empty(root.GetProperty("modules").EnumerateArray());
    }

    /// <summary>
    /// The counter is what will let a torn multi-file save be spotted instead of half-loaded, so it
    /// has to move forward on every commit and start from a known point.
    /// </summary>
    [Fact]
    public async Task Advances_the_generation_on_every_save()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);
        Assert.Equal(1, ReadManifest(campaign).GetProperty("generation").GetInt64());

        await _repository.SaveAsync(campaign);
        await _repository.SaveAsync(campaign);
        Assert.Equal(3, ReadManifest(campaign).GetProperty("generation").GetInt64());
    }

    [Fact]
    public async Task Leaves_no_temporary_file_behind()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);
        await _repository.SaveAsync(campaign);

        Assert.Empty(Directory.EnumerateFiles(_library.CampaignDirectory(campaign.Id.Value), "*.tmp"));
    }

    /// <summary>The first save has nothing to preserve; every later one does.</summary>
    [Fact]
    public async Task Starts_backing_up_only_once_there_is_something_to_lose()
    {
        var campaign = NewCampaign();
        var backups = BackupRoot(campaign);

        await _repository.SaveAsync(campaign);
        Assert.False(Directory.Exists(backups));

        await _repository.SaveAsync(campaign);
        Assert.Single(Directory.EnumerateDirectories(backups));
    }

    /// <summary>
    /// A backup is a whole generation, not a loose file: restoring half of one would be worse than
    /// restoring nothing once modules keep state files beside the manifest.
    /// </summary>
    [Fact]
    public async Task Keeps_each_backup_as_a_complete_generation()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);
        await _repository.SaveAsync(campaign);

        var backup = Assert.Single(Directory.EnumerateDirectories(BackupRoot(campaign)));

        Assert.Equal("gen-000001", Path.GetFileName(backup));
        Assert.True(File.Exists(Path.Combine(backup, "campaign.json")));
    }

    [Fact]
    public async Task Rotates_backups_so_the_directory_cannot_grow_without_limit()
    {
        var campaign = NewCampaign();

        for (var save = 0; save < 9; save++)
        {
            await _repository.SaveAsync(campaign);
        }

        Assert.Equal(5, Directory.EnumerateDirectories(BackupRoot(campaign)).Count());
    }

    private string BackupRoot(Campaign campaign)
        => Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "backups");

    private JsonElement ReadManifest(Campaign campaign)
        => JsonDocument.Parse(File.ReadAllText(_library.DocumentPath(campaign.Id.Value))).RootElement;
}
