using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Core.Tests.Entries.Fakes;
using DungeonApp.Core.World;
using DungeonApp.Testing;

namespace DungeonApp.Core.Tests.World;

public sealed class WorldTests : IDisposable
{
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));
    private static readonly EntityResolver Resolver = new(new ContentRegistry([], [], [], []), FakeContentTypeCatalog.Empty());

    private readonly TemporaryLibrary _library = new();

    public void Dispose() => _library.Dispose();

    private static CampaignStateSnapshot Apply(CampaignStateSnapshot snapshot, CampaignChange change) => snapshot.Apply(change);

    private static CampaignStateSnapshot AddFolder(CampaignStateSnapshot snapshot, FolderId? parent, string name, out FolderId id)
    {
        var added = WorldChanges.CreateFolder(snapshot, parent, name);
        id = added.Id;
        return snapshot.Apply(added.Change);
    }

    private static CampaignStateSnapshot AddEntities(
        CampaignStateSnapshot snapshot, EntryAddress source, string? label, int count, FolderId? folder, out IReadOnlyList<EntityId> ids)
    {
        var added = WorldChanges.AddEntities(snapshot, source, label, count, folder);
        ids = added.Ids;
        return snapshot.Apply(added.Change);
    }

    private static IReadOnlyDictionary<string, CampaignEntity> Entities(CampaignStateSnapshot snapshot) =>
        snapshot.Get(EntitiesModel.Declaration);

    // ---- adding ----

    [Fact]
    public void Adding_several_entities_numbers_them_in_a_row_and_moves_the_counter_in_the_same_change()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 3, null, out var ids);

        Assert.Equal([1, 2, 3], ids.Select(id => Entities(snapshot)[id.ToString()].Number));
        Assert.Equal(3, snapshot.Get(WorldModels.Counter)[WorldCounter.SingletonId].LastNumber);
    }

    [Fact]
    public void Adding_puts_entities_in_the_given_folder_and_keeps_their_names_empty()
    {
        var withFolder = AddFolder(CampaignStateSnapshot.Empty, null, "Karczma", out var tavern);
        var snapshot = AddEntities(withFolder, Goblin, null, 2, tavern, out var ids);

        Assert.All(ids, id =>
        {
            Assert.Equal(tavern, Entities(snapshot)[id.ToString()].FolderId);
            Assert.Null(Entities(snapshot)[id.ToString()].Label);
        });
    }

    [Fact]
    public void Adding_into_a_folder_that_does_not_exist_is_refused()
    {
        Assert.Throws<ArgumentException>(() =>
            WorldChanges.AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 1, FolderId.New()));
    }

    [Fact]
    public void A_number_is_never_handed_out_again_after_the_highest_entity_is_deleted()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 2, null, out var ids);
        snapshot = snapshot.Apply(WorldChanges.DeleteEntities([ids[1]]));

        snapshot = AddEntities(snapshot, Sword, null, 1, null, out var next);

        Assert.Equal(3, Entities(snapshot)[next[0].ToString()].Number);
    }

    [Fact]
    public void The_single_entity_add_takes_the_next_number_too()
    {
        var first = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(CampaignStateSnapshot.Empty, Goblin, null));
        var second = first.Apply(CampaignEntityChanges.Add(first, Goblin, null));

        Assert.Equal([1, 2], Entities(second).Values.Select(entity => entity.Number).Order());
    }

    // ---- folders ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_folder_name_is_refused_with_a_reason(string? name)
    {
        Assert.NotNull(WorldChanges.FolderNameProblem(name));
        Assert.Throws<InvalidOperationException>(() => WorldChanges.CreateFolder(CampaignStateSnapshot.Empty, null, name));
    }

    [Fact]
    public void Renaming_a_folder_keeps_its_place_and_trims_the_name()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "Lochy", out var dungeon);
        snapshot = AddFolder(snapshot, dungeon, "Piwnica", out var cellar);

        snapshot = snapshot.Apply(WorldChanges.RenameFolder(snapshot, cellar, "  Skarbiec "));

        var renamed = snapshot.Get(WorldModels.Folders)[cellar.ToString()];
        Assert.Equal("Skarbiec", renamed.Name);
        Assert.Equal(dungeon, renamed.ParentId);
        Assert.Throws<InvalidOperationException>(() => WorldChanges.RenameFolder(snapshot, cellar, " "));
    }

    [Fact]
    public void Renaming_an_entity_goes_through_relabel()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 1, null, out var ids);
        var entity = Entities(snapshot)[ids[0].ToString()];

        snapshot = snapshot.Apply(CampaignEntityChanges.Relabel(entity, "Krzywy"));

        Assert.Equal("Krzywy", WorldTree.Read(snapshot, Resolver).Entity(ids[0])!.Name);
    }

    // ---- moving ----

    [Fact]
    public void Moving_a_folder_takes_its_contents_along_and_entities_change_folder()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "A", out var a);
        snapshot = AddFolder(snapshot, null, "B", out var b);
        snapshot = AddEntities(snapshot, Goblin, null, 1, a, out var inA);
        snapshot = AddEntities(snapshot, Sword, null, 1, null, out var loose);

        snapshot = snapshot.Apply(WorldChanges.Move(snapshot, [a], [loose[0]], b));

        Assert.Equal(b, snapshot.Get(WorldModels.Folders)[a.ToString()].ParentId);
        Assert.Equal(b, Entities(snapshot)[loose[0].ToString()].FolderId);
        Assert.Equal(a, Entities(snapshot)[inA[0].ToString()].FolderId);
    }

    [Fact]
    public void A_folder_can_go_back_to_the_root()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "A", out var a);
        snapshot = AddFolder(snapshot, a, "B", out var b);

        snapshot = snapshot.Apply(WorldChanges.Move(snapshot, [b], [], null));

        Assert.Null(snapshot.Get(WorldModels.Folders)[b.ToString()].ParentId);
    }

    [Fact]
    public void A_folder_cannot_enter_itself_or_its_descendant()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "A", out var a);
        snapshot = AddFolder(snapshot, a, "B", out var b);
        snapshot = AddFolder(snapshot, b, "C", out var c);

        Assert.Equal("Katalog nie może trafić do samego siebie ani do swojego katalogu.", WorldChanges.MoveProblem(snapshot, [a], [], a));
        Assert.NotNull(WorldChanges.MoveProblem(snapshot, [a], [], c));
        Assert.Throws<InvalidOperationException>(() => WorldChanges.Move(snapshot, [a], [], c));
        Assert.Null(WorldChanges.MoveProblem(snapshot, [c], [], a));
    }

    [Fact]
    public void Moving_nothing_or_into_a_missing_folder_is_refused()
    {
        Assert.NotNull(WorldChanges.MoveProblem(CampaignStateSnapshot.Empty, [], [], null));
        Assert.NotNull(WorldChanges.MoveProblem(CampaignStateSnapshot.Empty, [], [EntityId.New()], FolderId.New()));
    }

    // ---- deleting ----

    [Fact]
    public void Several_entities_are_deleted_in_one_change()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 3, null, out var ids);

        snapshot = snapshot.Apply(WorldChanges.DeleteEntities([ids[0], ids[2]]));

        Assert.Equal([ids[1].ToString()], Entities(snapshot).Keys);
    }

    [Fact]
    public void Only_an_empty_folder_can_be_deleted()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "A", out var a);
        snapshot = AddFolder(snapshot, a, "B", out var b);
        snapshot = AddEntities(snapshot, Goblin, null, 1, b, out var ids);

        Assert.Equal("Katalog nie jest pusty.", WorldChanges.DeleteFolderProblem(snapshot, a));
        Assert.Equal("Katalog nie jest pusty.", WorldChanges.DeleteFolderProblem(snapshot, b));
        Assert.Throws<InvalidOperationException>(() => WorldChanges.DeleteFolders(snapshot, [b]));

        snapshot = snapshot.Apply(WorldChanges.DeleteEntities(ids));
        Assert.Null(WorldChanges.DeleteFolderProblem(snapshot, b));
        snapshot = snapshot.Apply(WorldChanges.DeleteFolders(snapshot, [b]));
        Assert.Null(WorldChanges.DeleteFolderProblem(snapshot, a));
    }

    // ---- reading the tree ----

    [Fact]
    public void Children_list_folders_first_then_entities_with_numbers_read_as_numbers()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, "Goblin 10", 1, null, out _);
        snapshot = AddEntities(snapshot, Goblin, "Goblin 2", 1, null, out _);
        snapshot = AddEntities(snapshot, Goblin, "goblin 1", 1, null, out _);
        snapshot = AddFolder(snapshot, null, "Żaby", out _);
        snapshot = AddFolder(snapshot, null, "Zamek", out _);

        var names = WorldTree.Read(snapshot, Resolver).Children(null).Select(node => node switch
        {
            FolderNode folder => "[" + folder.Folder.Name + "]",
            EntityNode entity => entity.Name,
            _ => throw new InvalidOperationException(),
        });

        Assert.Equal(["[Zamek]", "[Żaby]", "goblin 1", "Goblin 2", "Goblin 10"], names);
    }

    [Theory]
    [InlineData("Goblin 2", "Goblin 10", -1)]
    [InlineData("Goblin 10", "Goblin 2", 1)]
    [InlineData("Goblin 02", "Goblin 2", 0)]
    [InlineData("abc", "ABD", -1)]
    [InlineData("Goblin", "Goblin 1", -1)]
    public void Natural_order_compares_digit_runs_as_numbers(string x, string y, int sign) =>
        Assert.Equal(sign, Math.Sign(NaturalTextComparer.Instance.Compare(x, y)));

    [Fact]
    public void An_entity_whose_entry_is_gone_is_named_after_the_last_segment_of_its_address()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 1, null, out var ids);

        Assert.Equal("goblin", WorldTree.Read(snapshot, Resolver).Entity(ids[0])!.Name);
    }

    [Fact]
    public void Paths_run_from_the_root_folder_down()
    {
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "Lochy", out var dungeon);
        snapshot = AddFolder(snapshot, dungeon, "Piwnica", out var cellar);
        snapshot = AddEntities(snapshot, Goblin, null, 1, cellar, out var ids);
        snapshot = AddEntities(snapshot, Goblin, null, 1, null, out var rootIds);

        var tree = WorldTree.Read(snapshot, Resolver);

        Assert.Equal(["Lochy", "Piwnica"], tree.EntityPath(ids[0]));
        Assert.Empty(tree.EntityPath(rootIds[0]));
        Assert.Equal(["Lochy"], tree.FolderPath(dungeon));
    }

    [Fact]
    public void An_entity_in_a_missing_folder_is_shown_in_the_root()
    {
        var snapshot = AddEntities(CampaignStateSnapshot.Empty, Goblin, null, 1, null, out var ids);
        var orphan = Entities(snapshot)[ids[0].ToString()] with { FolderId = FolderId.New() };

        var tree = WorldTree.Read(snapshot.Apply(new CampaignChange().Upsert(EntitiesModel.Declaration, orphan)), Resolver);

        Assert.Single(tree.Children(null));
    }

    // ---- models and persistence ----

    [Fact]
    public void Combine_puts_the_frame_models_first_and_refuses_a_clash()
    {
        var extra = new StateModelDeclaration<WorldCounter>("system.extra", 1);

        Assert.Equal(
            [EntitiesModel.Declaration.ModelId, WorldModels.Folders.ModelId, WorldModels.Counter.ModelId, "system.extra"],
            WorldModels.Combine([extra]).Select(model => model.ModelId));
        Assert.Throws<ArgumentException>(() => WorldModels.Combine([WorldModels.Folders]));
    }

    private JsonCampaignRepository NewRepository() =>
        new(DungeonApp.Core.Systems.SystemId.Create("test-system"), _library.Path, path => Directory.Delete(path, recursive: true));

    [Fact]
    public async Task Folders_the_counter_and_entity_placement_survive_save_and_reopen()
    {
        var repository = NewRepository();
        var declarations = WorldModels.Combine([]);
        var snapshot = AddFolder(CampaignStateSnapshot.Empty, null, "Lochy", out var dungeon);
        snapshot = AddEntities(snapshot, Goblin, "Krzywy", 2, dungeon, out var ids);
        var campaign = Campaign.Create(CampaignName.Create("Kroniki"), TimeProvider.System).WithSnapshot(snapshot);

        await repository.SaveAsync(campaign, declarations);
        var reopened = (await repository.GetAsync(campaign.Id, declarations))!;

        Assert.Equal("Lochy", reopened.Snapshot.Get(WorldModels.Folders)[dungeon.ToString()].Name);
        Assert.Equal(2, reopened.Snapshot.Get(WorldModels.Counter)[WorldCounter.SingletonId].LastNumber);
        var entity = Entities(reopened.Snapshot)[ids[1].ToString()];
        Assert.Equal(dungeon, entity.FolderId);
        Assert.Equal(2, entity.Number);
    }

    private async Task<Campaign> RestoreLegacyAsync(params (Guid Id, string Label)[] entities)
    {
        var campaign = Campaign.Create(CampaignName.Create("Stara"), TimeProvider.System);
        var repository = NewRepository();

        // The state file a build without folders and numbers wrote: no folderId, no number.
        await repository.SaveAsync(campaign, [EntitiesModel.Declaration]);
        var path = Path.Combine(_library.CampaignDirectory(campaign.Id.Value), "state", "entries.instances.json");
        var records = string.Join(
            ",",
            entities.Select(e => $$$"""{"id":"{{{e.Id}}}","source":{"pack":"bestiary","entry":"goblin"},"label":"{{{e.Label}}}","patch":{}}"""));
        await File.WriteAllTextAsync(path, $$"""{"version":1,"generation":1,"records":[{{records}}]}""");

        return (await repository.GetAsync(campaign.Id, WorldModels.Combine([])))!;
    }

    [Fact]
    public async Task A_campaign_saved_without_folders_or_numbers_opens_with_stable_numbers_after_the_highest_id_order()
    {
        var low = new Guid("00000000-0000-0000-0000-000000000001");
        var high = new Guid("ffffffff-0000-0000-0000-000000000001");

        var first = await RestoreLegacyAsync((high, "B"), (low, "A"));
        var again = await RestoreLegacyAsync((low, "A"), (high, "B"));

        foreach (var campaign in new[] { first, again })
        {
            var entities = Entities(campaign.Snapshot);
            Assert.Equal(1, entities[low.ToString()].Number);
            Assert.Equal(2, entities[high.ToString()].Number);
            Assert.All(entities.Values, entity => Assert.Null(entity.FolderId));
            Assert.Equal(2, WorldNumbering.LastNumber(campaign.Snapshot));
        }

        // The counter starts behind them: the next entity is 3.
        var next = AddEntities(first.Snapshot, Goblin, null, 1, null, out var ids);
        Assert.Equal(3, Entities(next)[ids[0].ToString()].Number);
    }

    [Fact]
    public async Task A_campaign_with_no_world_files_opens_empty()
    {
        var campaign = await RestoreLegacyAsync();

        Assert.Empty(Entities(campaign.Snapshot));
        Assert.Empty(campaign.Snapshot.Get(WorldModels.Folders));
        Assert.Equal(0, WorldNumbering.LastNumber(campaign.Snapshot));
    }

    [Fact]
    public void An_entity_record_without_the_new_fields_deserializes_with_defaults()
    {
        var json = """{"id":"27a5bad0-d2d5-4a08-9917-3306acd1832a","source":{"pack":"bestiary","entry":"goblin"},"label":null,"patch":{}}""";

        var entity = JsonSerializer.Deserialize<CampaignEntity>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        Assert.Null(entity.FolderId);
        Assert.Equal(0, entity.Number);
    }
}
