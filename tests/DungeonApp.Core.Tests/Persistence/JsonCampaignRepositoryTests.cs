using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

public sealed class JsonCampaignRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);

    private readonly TemporaryLibrary _library = new();
    private readonly JsonCampaignRepository _repository;

    // Permanent delete on purpose: a test must never be able to reach the real Recycle Bin - see
    // JsonCampaignRepository.DeleteAsync's doc comment.
    public JsonCampaignRepositoryTests() =>
        _repository = new JsonCampaignRepository(_library.Path, path => Directory.Delete(path, recursive: true));

    public void Dispose() => _library.Dispose();

    private static Campaign NewCampaign(string name = "Kroniki Doliny") =>
        Campaign.Create(CampaignName.Create(name), new FixedTimeProvider(Moment));

    [Fact]
    public async Task Reads_back_everything_it_wrote()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);
        var restored = await _repository.GetAsync(campaign.Id, []);

        Assert.NotNull(restored);
        Assert.Equal(campaign.Id, restored.Id);
        Assert.Equal(campaign.Name, restored.Name);
        Assert.Equal(campaign.CreatedAt, restored.CreatedAt);
    }

    /// <summary>docs/architecture.md, "Kampania należy do jednego systemu": the manifest carries the campaign's system.</summary>
    [Fact]
    public async Task Persists_the_campaigns_system()
    {
        var systemId = DungeonApp.Core.Systems.SystemId.Create("dnd5e");
        var campaign = Campaign.Create(CampaignName.Create("Kroniki Doliny"), new FixedTimeProvider(Moment), systemId);

        await _repository.SaveAsync(campaign, []);
        var restored = await _repository.GetAsync(campaign.Id, []);

        Assert.Equal(systemId, restored!.SystemId);

        var root = ReadManifest(campaign);
        Assert.Equal("dnd5e", root.GetProperty("system").GetString());
    }

    /// <summary>
    /// A manifest that never recorded a system - including every one written before this field
    /// existed - reads back as a campaign without one, never guessed at.
    /// </summary>
    [Fact]
    public async Task Reads_a_manifest_without_a_system_field_as_no_system()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);
        var restored = await _repository.GetAsync(campaign.Id, []);

        Assert.Null(restored!.SystemId);
    }

    /// <summary>
    /// The directory is keyed by identity, not by the editable label. This is what will let a rename
    /// stay a rename instead of orphaning the campaign on disk.
    /// </summary>
    [Fact]
    public async Task Keys_the_directory_by_identity()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);

        Assert.True(File.Exists(_library.DocumentPath(campaign.Id.Value)));
    }

    [Fact]
    public async Task Returns_null_for_a_campaign_that_is_not_there()
        => Assert.Null(await _repository.GetAsync(CampaignId.New(), []));

    [Fact]
    public async Task Reports_an_empty_shelf_before_the_library_exists()
    {
        var repository = new JsonCampaignRepository(
            Path.Combine(_library.Path, "not-created-yet"), path => Directory.Delete(path, recursive: true));

        Assert.Empty(await repository.ListAsync());
    }

    [Fact]
    public async Task Lists_campaigns_in_a_stable_order()
    {
        await _repository.SaveAsync(NewCampaign("Zamek Nocy"), []);
        await _repository.SaveAsync(NewCampaign("Kroniki Doliny"), []);
        await _repository.SaveAsync(NewCampaign("Mokradła"), []);

        var names = (await _repository.ListAsync()).Select(summary => summary.Name.Value).ToArray();

        Assert.Equal(["Kroniki Doliny", "Mokradła", "Zamek Nocy"], names);
    }

    [Fact]
    public async Task Lists_a_campaigns_system()
    {
        var systemId = DungeonApp.Core.Systems.SystemId.Create("dnd5e");
        await _repository.SaveAsync(
            Campaign.Create(CampaignName.Create("Kroniki Doliny"), new FixedTimeProvider(Moment), systemId), []);

        var summary = Assert.Single(await _repository.ListAsync());

        Assert.Equal(systemId, summary.SystemId);
        Assert.Null(summary.ManifestFailure);
    }

    /// <summary>
    /// docs/architecture.md, "Kampania należy do jednego systemu": a pre-system manifest - any format
    /// version below the current one - is a campaign without a system, not a broken one. It is listed
    /// like any other, using whatever name it does carry.
    /// </summary>
    [Fact]
    public async Task Lists_a_legacy_manifest_as_a_campaign_with_no_system()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": 1,
              "id": "{{id}}",
              "name": "Kroniki Doliny",
              "createdAt": "2026-08-27T18:30:00+00:00",
              "generation": 1,
              "instances": []
            }
            """);

        var summary = Assert.Single(await _repository.ListAsync());

        Assert.Equal(new CampaignId(id), summary.Id);
        Assert.Equal("Kroniki Doliny", summary.Name.Value);
        Assert.Null(summary.SystemId);
        Assert.Null(summary.ManifestFailure);
    }

    /// <summary>A manifest from a future build is listed too, flagged rather than skipped or half-read.</summary>
    [Fact]
    public async Task Lists_a_manifest_from_a_newer_build_as_unavailable()
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

        var summary = Assert.Single(await _repository.ListAsync());

        Assert.Equal(CampaignStoreFailure.UnsupportedFormatVersion, summary.ManifestFailure);
    }

    /// <summary>
    /// One broken document must not hide every healthy campaign beside it - and must not disappear
    /// either. docs/architecture.md, "Kampania należy do jednego systemu": "Kampania, której systemu
    /// nie ma w programie, jest widoczna jako niedostępna - nie znika." The same holds for one whose
    /// manifest cannot be read at all.
    /// </summary>
    [Fact]
    public async Task Lists_a_damaged_campaign_as_unavailable_instead_of_hiding_it()
    {
        await _repository.SaveAsync(NewCampaign("Kroniki Doliny"), []);
        var damagedId = Guid.NewGuid();
        _library.WriteDocument(damagedId, "{ this is not json");

        var summaries = await _repository.ListAsync();

        Assert.Equal(2, summaries.Count);

        var healthy = Assert.Single(summaries, summary => summary.Name.Value == "Kroniki Doliny");
        Assert.Null(healthy.ManifestFailure);

        var damaged = Assert.Single(summaries, summary => summary.Id == new CampaignId(damagedId));
        Assert.Equal(CampaignStoreFailure.Unreadable, damaged.ManifestFailure);
        // No name could be read back, so the directory's own name - the campaign's id - stands in.
        Assert.Equal(damagedId.ToString("D"), damaged.Name.Value);
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
            () => _repository.GetAsync(new CampaignId(id), []));

        Assert.Equal(CampaignStoreFailure.UnsupportedFormatVersion, exception.Failure);
    }

    /// <summary>docs/architecture.md, "Wersjonowanie": no migration exists, so an older format is refused - distinguishably from a newer one - rather than guessed at.</summary>
    [Fact]
    public async Task Refuses_a_document_written_by_an_older_build()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": {{JsonCampaignRepository.CurrentFormatVersion - 1}},
              "id": "{{id}}",
              "name": "Z przeszłości",
              "createdAt": "2026-08-27T18:30:00+00:00"
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id), []));

        Assert.Equal(CampaignStoreFailure.LegacyFormatVersion, exception.Failure);
    }

    [Fact]
    public async Task Reports_unreadable_json_as_such()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, "{ this is not json");

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id), []));

        Assert.Equal(CampaignStoreFailure.Unreadable, exception.Failure);
    }

    [Fact]
    public async Task Rejects_a_document_that_carries_no_usable_name()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": {{JsonCampaignRepository.CurrentFormatVersion}},
              "id": "{{id}}",
              "name": "   ",
              "createdAt": "2026-08-27T18:30:00+00:00"
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id), []));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    /// <summary>
    /// A field nobody reads is not free: it invites a future build to treat it as meaningful. The
    /// manifest carries what this build actually writes and nothing held open for a later one.
    /// </summary>
    [Fact]
    public async Task Writes_no_manifest_field_that_nothing_reads()
    {
        var campaign = NewCampaign();
        await _repository.SaveAsync(campaign, []);

        var root = ReadManifest(campaign);

        Assert.Equal(JsonCampaignRepository.CurrentFormatVersion, root.GetProperty("formatVersion").GetInt32());
        Assert.False(root.TryGetProperty("ruleset", out _));
        Assert.False(root.TryGetProperty("contentPacks", out _));
    }

    /// <summary>
    /// docs/architecture.md, "Wersjonowanie": "Migracji nie budujemy" - a manifest from the format
    /// this build's predecessor wrote (one file per instance, no state models) is refused
    /// distinguishably rather than half-read or silently reinterpreted.
    /// </summary>
    [Fact]
    public async Task Refuses_a_manifest_from_the_pre_state_model_format()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": 1,
              "id": "{{id}}",
              "name": "Kroniki Doliny",
              "createdAt": "2026-08-27T18:30:00+00:00",
              "generation": 1,
              "instances": []
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(new CampaignId(id), []));

        Assert.Equal(CampaignStoreFailure.LegacyFormatVersion, exception.Failure);
    }

    /// <summary>
    /// The counter is what will let a torn multi-file save be spotted instead of half-loaded, so it
    /// has to move forward on every commit and start from a known point.
    /// </summary>
    [Fact]
    public async Task Advances_the_generation_on_every_save()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);
        Assert.Equal(1, ReadManifest(campaign).GetProperty("generation").GetInt64());

        await _repository.SaveAsync(campaign, []);
        await _repository.SaveAsync(campaign, []);
        Assert.Equal(3, ReadManifest(campaign).GetProperty("generation").GetInt64());
    }

    [Fact]
    public async Task Leaves_no_temporary_file_behind()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);
        await _repository.SaveAsync(campaign, []);

        Assert.Empty(Directory.EnumerateFiles(_library.CampaignDirectory(campaign.Id.Value), "*.tmp"));
    }

    private JsonElement ReadManifest(Campaign campaign)
        => JsonDocument.Parse(File.ReadAllText(_library.DocumentPath(campaign.Id.Value))).RootElement;
}
