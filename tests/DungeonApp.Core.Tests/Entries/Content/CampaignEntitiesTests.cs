using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Tests.Entries.Content;

/// <summary>
/// <see cref="CampaignEntityChanges"/> only builds changes; applying one is
/// <see cref="CampaignStateSnapshot.Apply"/>'s job, exercised here through the pair the same way a
/// caller (a tool view model) always uses them.
/// </summary>
public sealed class CampaignEntitiesTests
{
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));

    private static CampaignEntity Single(CampaignStateSnapshot snapshot) =>
        Assert.Single(snapshot.Get(EntitiesModel.Declaration).Values);

    [Fact]
    public void Add_creates_an_entity_with_an_empty_patch_and_a_fresh_id()
    {
        var snapshot = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, "Krzywy"));

        var created = Single(snapshot);
        Assert.NotEqual(default, created.Id);
        Assert.Equal(Goblin, created.Source);
        Assert.Equal("Krzywy", created.Label);
        Assert.True(created.Patch.IsEmpty);
    }

    [Fact]
    public void Two_adds_of_the_same_entry_produce_two_different_entities()
    {
        var snapshot = CampaignStateSnapshot.Empty
            .Apply(CampaignEntityChanges.Add(Goblin, null))
            .Apply(CampaignEntityChanges.Add(Goblin, null));

        var entities = snapshot.Get(EntitiesModel.Declaration);
        Assert.Equal(2, entities.Count);
    }

    [Fact]
    public void Remove_deletes_the_entity()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, null));
        var created = Single(afterAdd);

        var snapshot = afterAdd.Apply(CampaignEntityChanges.Remove(created.Id));

        Assert.Empty(snapshot.Get(EntitiesModel.Declaration));
    }

    [Fact]
    public void Removing_an_unknown_id_leaves_every_other_entity_untouched()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, "Krzywy"));

        var snapshot = afterAdd.Apply(CampaignEntityChanges.Remove(EntityId.New()));

        Assert.Equal(afterAdd.Get(EntitiesModel.Declaration).Keys, snapshot.Get(EntitiesModel.Declaration).Keys);
    }

    [Fact]
    public void A_change_touching_one_entity_leaves_the_other_untouched()
    {
        var afterFirst = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, "Pierwszy"));
        var firstEntity = Single(afterFirst);
        var afterBoth = afterFirst.Apply(CampaignEntityChanges.Add(Sword, "Drugi"));
        var secondEntity = afterBoth.Get(EntitiesModel.Declaration).Values.Single(i => i.Id != firstEntity.Id);

        // A change that only names the second entity must not disturb the first one at all - the
        // fourth ban read as a data structure: nothing moves that was not handed to the change.
        var afterRelabel = afterBoth.Apply(CampaignEntityChanges.Relabel(secondEntity, "Nowa nazwa"));

        Assert.Equal(firstEntity, afterRelabel.Get(EntitiesModel.Declaration)[firstEntity.Id.ToString()]);
    }

    [Fact]
    public void Relabel_changes_only_the_label()
    {
        var patch = ContentValues.From(new Dictionary<string, object> { ["hp"] = 3 });
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, "Krzywy"));
        var afterPatch = afterAdd.Apply(CampaignEntityChanges.ReplacePatch(Single(afterAdd), patch));

        var relabelled = afterPatch.Apply(CampaignEntityChanges.Relabel(Single(afterPatch), "Zguba"));

        var found = Single(relabelled);
        Assert.Equal("Zguba", found.Label);
        Assert.Same(patch, found.Patch);
    }

    [Fact]
    public void ReplacePatch_changes_only_the_patch()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Sword, "Zguba"));
        var created = Single(afterAdd);
        var patch = ContentValues.From(new Dictionary<string, object> { ["quantity"] = 2 });

        var snapshot = afterAdd.Apply(CampaignEntityChanges.ReplacePatch(created, patch));

        var found = Single(snapshot);
        Assert.Equal("Zguba", found.Label);
        Assert.Same(patch, found.Patch);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_whitespace_only_label_becomes_null_on_add(string? label)
    {
        var created = Single(CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, label)));

        Assert.Null(created.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_whitespace_only_label_becomes_null_on_relabel(string? label)
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignEntityChanges.Add(Goblin, "Krzywy"));
        var created = Single(afterAdd);

        var snapshot = afterAdd.Apply(CampaignEntityChanges.Relabel(created, label));

        Assert.Null(Single(snapshot).Label);
    }

    [Fact]
    public void A_freshly_created_campaign_has_no_entities()
    {
        Assert.Empty(CampaignStateSnapshot.Empty.Get(EntitiesModel.Declaration));
    }
}
