using System.Collections.Generic;
using System.Linq;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Instances;
using DungeonApp.Core.State;

namespace DungeonApp.Library.Entries.Tests.Content;

/// <summary>
/// <see cref="CampaignInstanceChanges"/> only builds changes; applying one is
/// <see cref="CampaignStateSnapshot.Apply"/>'s job, exercised here through the pair the same way a
/// caller (a tool view model) always uses them.
/// </summary>
public sealed class CampaignInstancesTests
{
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));

    private static CampaignInstance Single(CampaignStateSnapshot snapshot) =>
        Assert.Single(snapshot.Get(InstancesModel.Declaration).Values);

    [Fact]
    public void Add_creates_an_instance_with_an_empty_patch_and_a_fresh_id()
    {
        var snapshot = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, "Krzywy"));

        var created = Single(snapshot);
        Assert.NotEqual(default, created.Id);
        Assert.Equal(Goblin, created.Source);
        Assert.Equal("Krzywy", created.Label);
        Assert.True(created.Patch.IsEmpty);
    }

    [Fact]
    public void Two_adds_of_the_same_entry_produce_two_different_instances()
    {
        var snapshot = CampaignStateSnapshot.Empty
            .Apply(CampaignInstanceChanges.Add(Goblin, null))
            .Apply(CampaignInstanceChanges.Add(Goblin, null));

        var instances = snapshot.Get(InstancesModel.Declaration);
        Assert.Equal(2, instances.Count);
    }

    [Fact]
    public void Remove_deletes_the_instance()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, null));
        var created = Single(afterAdd);

        var snapshot = afterAdd.Apply(CampaignInstanceChanges.Remove(created.Id));

        Assert.Empty(snapshot.Get(InstancesModel.Declaration));
    }

    [Fact]
    public void Removing_an_unknown_id_leaves_every_other_instance_untouched()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, "Krzywy"));

        var snapshot = afterAdd.Apply(CampaignInstanceChanges.Remove(InstanceId.New()));

        Assert.Equal(afterAdd.Get(InstancesModel.Declaration).Keys, snapshot.Get(InstancesModel.Declaration).Keys);
    }

    [Fact]
    public void A_change_touching_one_instance_leaves_the_other_untouched()
    {
        var afterFirst = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, "Pierwszy"));
        var firstInstance = Single(afterFirst);
        var afterBoth = afterFirst.Apply(CampaignInstanceChanges.Add(Sword, "Drugi"));
        var secondInstance = afterBoth.Get(InstancesModel.Declaration).Values.Single(i => i.Id != firstInstance.Id);

        // A change that only names the second instance must not disturb the first one at all - the
        // fourth ban read as a data structure: nothing moves that was not handed to the change.
        var afterRelabel = afterBoth.Apply(CampaignInstanceChanges.Relabel(secondInstance, "Nowa nazwa"));

        Assert.Equal(firstInstance, afterRelabel.Get(InstancesModel.Declaration)[firstInstance.Id.ToString()]);
    }

    [Fact]
    public void Relabel_changes_only_the_label()
    {
        var patch = ContentValues.From(new Dictionary<string, object> { ["hp"] = 3 });
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, "Krzywy"));
        var afterPatch = afterAdd.Apply(CampaignInstanceChanges.ReplacePatch(Single(afterAdd), patch));

        var relabelled = afterPatch.Apply(CampaignInstanceChanges.Relabel(Single(afterPatch), "Zguba"));

        var found = Single(relabelled);
        Assert.Equal("Zguba", found.Label);
        Assert.Same(patch, found.Patch);
    }

    [Fact]
    public void ReplacePatch_changes_only_the_patch()
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Sword, "Zguba"));
        var created = Single(afterAdd);
        var patch = ContentValues.From(new Dictionary<string, object> { ["quantity"] = 2 });

        var snapshot = afterAdd.Apply(CampaignInstanceChanges.ReplacePatch(created, patch));

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
        var created = Single(CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, label)));

        Assert.Null(created.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_whitespace_only_label_becomes_null_on_relabel(string? label)
    {
        var afterAdd = CampaignStateSnapshot.Empty.Apply(CampaignInstanceChanges.Add(Goblin, "Krzywy"));
        var created = Single(afterAdd);

        var snapshot = afterAdd.Apply(CampaignInstanceChanges.Relabel(created, label));

        Assert.Null(Single(snapshot).Label);
    }

    [Fact]
    public void A_freshly_created_campaign_has_no_instances()
    {
        Assert.Empty(CampaignStateSnapshot.Empty.Get(InstancesModel.Declaration));
    }
}
