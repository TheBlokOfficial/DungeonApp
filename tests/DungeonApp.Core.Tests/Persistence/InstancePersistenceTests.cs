using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// Kept apart from <see cref="JsonCampaignRepositoryTests"/>: one file per magazine the store
/// persists, rather than one growing file per store.
/// </summary>
public sealed class InstancePersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));

    private readonly TemporaryLibrary _library = new();
    private readonly JsonCampaignRepository _repository;

    public InstancePersistenceTests() => _repository = new JsonCampaignRepository(_library.Path);

    public void Dispose() => _library.Dispose();

    private static Campaign NewCampaign() =>
        Campaign.Create(CampaignName.Create("Kroniki Doliny"), new FixedTimeProvider(Moment));

    private string InstancesDirectory(Campaign campaign) =>
        Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "instances");

    private string InstanceFilePath(Campaign campaign, InstanceId id) =>
        Path.Combine(InstancesDirectory(campaign), $"{id.Value:D}.json");

    /// <summary>A record shaped the way a system's own patch record would be, used only to read a
    /// patch back through the same envelope API a system uses - <see cref="ContentValues.Read{T}"/>
    /// - rather than by inspecting raw JSON.</summary>
    private sealed record SamplePatch(int? Hp, string? Name, bool? Cursed, double? Multiplier, string[]? Tags);

    [Fact]
    public async Task Reads_back_an_instance_with_the_same_id_address_and_label()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, "Krzywy");

        await _repository.SaveAsync(campaign);
        var reopened = await _repository.GetAsync(campaign.Id);

        var found = reopened!.Instances.Find(created.Id);
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(Goblin, found.Source);
        Assert.Equal("Krzywy", found.Label);
    }

    /// <summary>The most load-bearing round-trip in this step: the sealed envelope has to carry every
    /// property back exactly as it went in, through the same <c>Read</c>/<c>From</c> pair a system
    /// uses, and nothing in the store may ever look at a property by name to get there.</summary>
    [Fact]
    public async Task A_non_empty_patch_round_trips_value_for_value()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, "Krzywy");
        var patch = ContentValues.From(new SamplePatch(Hp: 3, Name: "Zguba", Cursed: true, Multiplier: 1.5, Tags: ["elite", "boss"]));
        campaign.Instances.ReplacePatch(created.Id, patch);

        await _repository.SaveAsync(campaign);
        var reopened = await _repository.GetAsync(campaign.Id);

        var restored = reopened!.Instances.Find(created.Id)!.Patch.Read<SamplePatch>();
        Assert.Equal(3, restored.Hp);
        Assert.Equal("Zguba", restored.Name);
        Assert.Equal(true, restored.Cursed);
        Assert.Equal(1.5, restored.Multiplier);
        Assert.Equal(["elite", "boss"], restored.Tags!);
    }

    [Fact]
    public async Task An_instance_without_a_label_comes_back_null()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);

        await _repository.SaveAsync(campaign);
        var reopened = await _repository.GetAsync(campaign.Id);

        Assert.Null(reopened!.Instances.Find(created.Id)!.Label);
    }

    /// <summary>The one this whole step turns on: a deleted instance must never resurrect because its
    /// file merely survived on disk. The manifest is the index, so a file it does not list is unreachable
    /// whether or not the cleanup that removes it ever ran.</summary>
    [Fact]
    public async Task A_removed_instance_does_not_come_back_after_save_and_reopen()
    {
        var campaign = NewCampaign();
        var kept = campaign.Instances.Add(Goblin, "Krzywy");
        var removed = campaign.Instances.Add(Sword, "Zguba");
        await _repository.SaveAsync(campaign);

        campaign.Instances.Remove(removed.Id);
        await _repository.SaveAsync(campaign);

        var reopened = await _repository.GetAsync(campaign.Id);

        Assert.NotNull(reopened!.Instances.Find(kept.Id));
        Assert.Null(reopened.Instances.Find(removed.Id));
        Assert.Equal(kept.Id, Assert.Single(reopened.Instances.All).Id);
    }

    [Fact]
    public async Task The_file_for_a_removed_instance_is_gone_from_disk_after_the_next_save()
    {
        var campaign = NewCampaign();
        var removed = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);
        var filePath = InstanceFilePath(campaign, removed.Id);
        Assert.True(File.Exists(filePath));

        campaign.Instances.Remove(removed.Id);
        await _repository.SaveAsync(campaign);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task A_campaign_with_no_instances_creates_no_instances_directory()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign);

        Assert.False(Directory.Exists(InstancesDirectory(campaign)));
    }

    [Fact]
    public async Task A_manifest_written_by_an_older_build_without_an_instances_key_still_loads()
    {
        var id = Guid.NewGuid();
        _library.WriteDocument(id, $$"""
            {
              "formatVersion": 1,
              "id": "{{id}}",
              "name": "Kroniki Doliny",
              "createdAt": "2026-08-27T18:30:00+00:00",
              "generation": 1,
              "dataBlocks": []
            }
            """);

        var restored = await _repository.GetAsync(new CampaignId(id));

        Assert.NotNull(restored);
        Assert.Empty(restored.Instances.All);
    }

    [Fact]
    public async Task An_instance_named_in_the_manifest_but_missing_from_disk_throws()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        File.Delete(InstanceFilePath(campaign, created.Id));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task An_instance_file_at_a_generation_other_than_the_manifest_throws()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        var path = InstanceFilePath(campaign, created.Id);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"generation\": 1", "\"generation\": 0"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task An_instance_file_with_invalid_json_throws()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        File.WriteAllText(InstanceFilePath(campaign, created.Id), "{ this is not json");

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.Unreadable, exception.Failure);
    }

    /// <summary>A pack or entry id that no longer fits <see cref="ContentId"/> is reported as
    /// <see cref="CampaignStoreFailure.Invalid"/> rather than surfacing as a raw deserialization
    /// failure that cannot say which instance is at fault.</summary>
    [Fact]
    public async Task An_instance_file_naming_an_unusable_pack_id_throws_Invalid()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        var path = InstanceFilePath(campaign, created.Id);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"bestiary\"", "\"NOT VALID:\""));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    /// <summary>Valid JSON that simply has no patch at all - a hand-edited or half-written file. The
    /// store has to name it as a damaged campaign file like any other, not let a deserialization detail
    /// escape as whatever exception the serializer happens to raise.</summary>
    [Fact]
    public async Task An_instance_file_carrying_no_patch_at_all_throws()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        File.WriteAllText(InstanceFilePath(campaign, created.Id), $$"""
            {
              "instanceId": "{{created.Id.Value:D}}",
              "packId": "bestiary",
              "entryId": "goblin",
              "label": null,
              "generation": 1
            }
            """);

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.Unreadable, exception.Failure);
    }

    /// <summary>A patch that is a number, a string or an array cannot be laid over an entry's values at
    /// all. Refused on the way in, where the message can still name the instance, rather than much later
    /// inside whatever first tries to merge it.</summary>
    [Fact]
    public async Task An_instance_file_whose_patch_is_not_an_object_throws()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);
        await _repository.SaveAsync(campaign);

        var path = InstanceFilePath(campaign, created.Id);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"patch\": {}", "\"patch\": 7"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    [Fact]
    public async Task Manifest_lists_each_instance_with_its_id_and_generation()
    {
        var campaign = NewCampaign();
        var created = campaign.Instances.Add(Goblin, null);

        await _repository.SaveAsync(campaign);

        var manifestPath = _library.DocumentPath(campaign.Id.Value);
        var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath)).RootElement;
        var entry = Assert.Single(manifest.GetProperty("instances").EnumerateArray());
        Assert.Equal(created.Id.Value.ToString("D"), entry.GetProperty("id").GetString(), ignoreCase: true);
        Assert.Equal(1, entry.GetProperty("generation").GetInt64());
    }
}
