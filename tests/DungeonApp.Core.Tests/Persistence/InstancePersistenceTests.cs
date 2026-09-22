using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Content.Instances;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Persistence;

/// <summary>
/// Instances as the concrete example of a state model - the same round trip any future model gets
/// for free from <see cref="JsonCampaignRepository"/>'s generic per-model file. Kept apart from
/// <see cref="JsonCampaignRepositoryTests"/>, which is about the manifest and the format version
/// rather than about any one model's own records.
/// </summary>
public sealed class InstancePersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 27, 18, 30, 0, TimeSpan.Zero);
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));
    private static readonly IReadOnlyList<StateModelDeclaration> Declarations = [InstancesModel.Declaration];

    private readonly TemporaryLibrary _library = new();
    private readonly JsonCampaignRepository _repository;

    public InstancePersistenceTests() => _repository = new JsonCampaignRepository(_library.Path);

    public void Dispose() => _library.Dispose();

    private static Campaign NewCampaign() =>
        Campaign.Create(CampaignName.Create("Kroniki Doliny"), new FixedTimeProvider(Moment));

    private static Campaign WithInstance(Campaign campaign, EntryAddress source, string? label, out CampaignInstance created)
    {
        var change = CampaignInstanceChanges.Add(source, label);
        var updated = campaign.WithSnapshot(campaign.Snapshot.Apply(change));
        created = updated.Snapshot.Get(InstancesModel.Declaration).Values.Single();
        return updated;
    }

    private string ModelPath(Campaign campaign) =>
        Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "state", $"{InstancesModel.Declaration.ModelId}.json");

    /// <summary>A record shaped the way a system's own patch record would be, used only to read a
    /// patch back through the same envelope API a system uses - <see cref="ContentValues.Read{T}"/>
    /// - rather than by inspecting raw JSON.</summary>
    private sealed record SamplePatch(int? Hp, string? Name, bool? Cursed, double? Multiplier, string[]? Tags);

    [Fact]
    public async Task Reads_back_an_instance_with_the_same_id_address_and_label()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, "Krzywy", out var created);

        await _repository.SaveAsync(campaign, Declarations);
        var reopened = await _repository.GetAsync(campaign.Id, Declarations);

        var found = reopened!.Snapshot.Get(InstancesModel.Declaration)[created.Id.ToString()];
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
        var campaign = WithInstance(NewCampaign(), Goblin, "Krzywy", out var created);
        var patch = ContentValues.From(new SamplePatch(Hp: 3, Name: "Zguba", Cursed: true, Multiplier: 1.5, Tags: ["elite", "boss"]));
        campaign = campaign.WithSnapshot(campaign.Snapshot.Apply(CampaignInstanceChanges.ReplacePatch(created, patch)));

        await _repository.SaveAsync(campaign, Declarations);
        var reopened = await _repository.GetAsync(campaign.Id, Declarations);

        var restored = reopened!.Snapshot.Get(InstancesModel.Declaration)[created.Id.ToString()].Patch.Read<SamplePatch>();
        Assert.Equal(3, restored.Hp);
        Assert.Equal("Zguba", restored.Name);
        Assert.Equal(true, restored.Cursed);
        Assert.Equal(1.5, restored.Multiplier);
        Assert.Equal(["elite", "boss"], restored.Tags!);
    }

    /// <summary>
    /// The state file is meant to be human-readable, not an object graph of wrapper types: an id is
    /// a plain string, and an entry address is a pair of plain strings, never <c>{"value":"…"}</c>.
    /// <see cref="ContentIdJsonConverter"/> and <see cref="InstanceIdJsonConverter"/> are what make
    /// that true without a hand-written DTO standing in for either value type.
    /// </summary>
    [Fact]
    public async Task Writes_ids_and_the_entry_address_as_flat_strings_and_round_trips()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, "Krzywy", out var created);

        await _repository.SaveAsync(campaign, Declarations);

        var document = JsonDocument.Parse(File.ReadAllText(ModelPath(campaign)));
        var record = Assert.Single(document.RootElement.GetProperty("records").EnumerateArray());

        Assert.Equal(JsonValueKind.String, record.GetProperty("id").ValueKind);
        Assert.Equal(created.Id.ToString(), record.GetProperty("id").GetString());

        var source = record.GetProperty("source");
        Assert.Equal(JsonValueKind.String, source.GetProperty("pack").ValueKind);
        Assert.Equal("bestiary", source.GetProperty("pack").GetString());
        Assert.Equal(JsonValueKind.String, source.GetProperty("entry").ValueKind);
        Assert.Equal("goblin", source.GetProperty("entry").GetString());

        var reopened = await _repository.GetAsync(campaign.Id, Declarations);
        var found = reopened!.Snapshot.Get(InstancesModel.Declaration)[created.Id.ToString()];

        Assert.Equal(created.Id, found.Id);
        Assert.Equal(Goblin, found.Source);
        Assert.Equal("Krzywy", found.Label);
    }

    /// <summary>An instance id that fails to parse as a GUID is exactly as invalid as a pack id whose charset is wrong - the same named failure, not a raw exception.</summary>
    [Fact]
    public async Task A_record_with_an_unparsable_instance_id_throws_Invalid()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out var created);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace($"\"{created.Id}\"", "\"not-a-guid\""));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    [Fact]
    public async Task An_instance_without_a_label_comes_back_null()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out var created);

        await _repository.SaveAsync(campaign, Declarations);
        var reopened = await _repository.GetAsync(campaign.Id, Declarations);

        Assert.Null(reopened!.Snapshot.Get(InstancesModel.Declaration)[created.Id.ToString()].Label);
    }

    /// <summary>The one this whole model turns on: a deleted instance must never resurrect because the
    /// model file merely survived on disk. The whole model file is rewritten on every save, so a
    /// removed record simply is not in it any more.</summary>
    [Fact]
    public async Task A_removed_instance_does_not_come_back_after_save_and_reopen()
    {
        var afterFirst = WithInstance(NewCampaign(), Goblin, "Krzywy", out var kept);
        var afterBoth = afterFirst.WithSnapshot(afterFirst.Snapshot.Apply(CampaignInstanceChanges.Add(Sword, "Zguba")));
        var removed = afterBoth.Snapshot.Get(InstancesModel.Declaration).Values.Single(i => i.Id != kept.Id);
        await _repository.SaveAsync(afterBoth, Declarations);

        var afterRemoval = afterBoth.WithSnapshot(afterBoth.Snapshot.Apply(CampaignInstanceChanges.Remove(removed.Id)));
        await _repository.SaveAsync(afterRemoval, Declarations);

        var reopened = await _repository.GetAsync(afterBoth.Id, Declarations);
        var instances = reopened!.Snapshot.Get(InstancesModel.Declaration);

        Assert.True(instances.ContainsKey(kept.Id.ToString()));
        Assert.False(instances.ContainsKey(removed.Id.ToString()));
        Assert.Single(instances);
    }

    [Fact]
    public async Task A_declared_model_with_no_records_still_gets_an_empty_state_file()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, Declarations);

        var document = JsonDocument.Parse(File.ReadAllText(ModelPath(campaign)));
        Assert.Empty(document.RootElement.GetProperty("records").EnumerateArray());
    }

    [Fact]
    public async Task No_declared_models_writes_no_state_directory()
    {
        var campaign = NewCampaign();

        await _repository.SaveAsync(campaign, []);

        Assert.False(Directory.Exists(Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "state")));
    }

    /// <summary>"Plik modelu, którego nikt nie zadeklarował → nieczytany, nietknięty na dysku." - a
    /// file for a model this build's declarations do not name is never opened, so a save that
    /// writes only the declared models must leave it byte for byte as it was.</summary>
    [Fact]
    public async Task A_state_file_for_an_undeclared_model_is_left_untouched_by_a_save()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, "Krzywy", out _);
        await _repository.SaveAsync(campaign, Declarations);

        var stateDirectory = Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "state");
        var foreignPath = Path.Combine(stateDirectory, "some-system.unknown-model.json");
        var foreignBytes = "this is not touched by anything this build understands"u8.ToArray();
        File.WriteAllBytes(foreignPath, foreignBytes);

        // Re-save with a further change to the declared model, to prove the untouched file is not
        // merely a fluke of nothing having changed.
        var afterAnotherAdd = campaign.WithSnapshot(campaign.Snapshot.Apply(CampaignInstanceChanges.Add(Sword, "Zguba")));
        await _repository.SaveAsync(afterAnotherAdd, Declarations);

        Assert.Equal(foreignBytes, File.ReadAllBytes(foreignPath));
    }

    [Fact]
    public async Task A_model_the_manifest_does_not_list_reads_as_empty()
    {
        var campaign = NewCampaign();
        await _repository.SaveAsync(campaign, []);

        var reopened = await _repository.GetAsync(campaign.Id, Declarations);

        Assert.Empty(reopened!.Snapshot.Get(InstancesModel.Declaration));
    }

    [Fact]
    public async Task A_model_listed_in_the_manifest_but_missing_its_state_file_throws()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        File.Delete(ModelPath(campaign));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task A_model_file_at_a_generation_other_than_the_manifest_throws()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"generation\": 1", "\"generation\": 0"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.TornSave, exception.Failure);
    }

    [Fact]
    public async Task A_model_file_at_a_version_the_declaration_does_not_declare_throws()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"version\": 1", "\"version\": 2"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.ModelVersionMismatch, exception.Failure);
    }

    [Fact]
    public async Task A_model_file_with_invalid_json_throws()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        File.WriteAllText(ModelPath(campaign), "{ this is not json");

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.Unreadable, exception.Failure);
    }

    /// <summary>A pack id that no longer fits <see cref="ContentId"/>'s charset is reported as
    /// <see cref="CampaignStoreFailure.Invalid"/> rather than surfacing as a raw deserialization
    /// failure - <see cref="ContentId"/>'s own constructor is the invariant's one enforcement point,
    /// called here by the deserializer instead of by <see cref="ContentId.Create"/>, and
    /// <c>System.Text.Json</c> wraps what it throws in a <see cref="JsonException"/> the same way it
    /// would any other converter failure.</summary>
    [Fact]
    public async Task A_record_naming_an_unusable_pack_id_throws_Invalid()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"bestiary\"", "\"NOT VALID:\""));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    /// <summary>Valid JSON that simply has no patch at all - a hand-edited or half-written record.
    /// The deserializer is the only validator for this model (docs/architecture.md,
    /// "Deserializator jest jedynym walidatorem"): a missing required property fails to deserialize
    /// the record, and the store reports that as an invalid record for the model rather than letting
    /// the exception escape unlabeled.</summary>
    [Fact]
    public async Task A_record_missing_its_required_patch_throws_Invalid()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out var created);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"patch\": {}", "\"patchGoneMissing\": {}"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    /// <summary>A patch that is a number, a string or an array cannot be an envelope at all - rejected
    /// by <c>ContentValues</c>'s own converter at deserialization time, which is what lets this
    /// surface as the same named failure as any other invalid record instead of a raw exception from
    /// whatever first tries to merge it.</summary>
    [Fact]
    public async Task A_record_whose_patch_is_not_an_object_throws_Invalid()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);
        await _repository.SaveAsync(campaign, Declarations);

        var path = ModelPath(campaign);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"patch\": {}", "\"patch\": 7"));

        var exception = await Assert.ThrowsAsync<CampaignStoreException>(
            () => _repository.GetAsync(campaign.Id, Declarations));

        Assert.Equal(CampaignStoreFailure.Invalid, exception.Failure);
    }

    [Fact]
    public async Task Manifest_lists_the_model_with_its_id_and_generation()
    {
        var campaign = WithInstance(NewCampaign(), Goblin, null, out _);

        await _repository.SaveAsync(campaign, Declarations);

        var manifestPath = _library.DocumentPath(campaign.Id.Value);
        var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath)).RootElement;
        var entry = Assert.Single(manifest.GetProperty("models").EnumerateArray());
        Assert.Equal(InstancesModel.Declaration.ModelId, entry.GetProperty("id").GetString());
        Assert.Equal(1, entry.GetProperty("generation").GetInt64());
    }
}
