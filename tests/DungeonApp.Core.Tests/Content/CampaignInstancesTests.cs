using System;
using System.Collections.Generic;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Content;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Tests.Content;

public sealed class CampaignInstancesTests
{
    private static readonly EntryAddress Goblin = new(ContentId.Create("bestiary"), ContentId.Create("goblin"));
    private static readonly EntryAddress Sword = new(ContentId.Create("gear"), ContentId.Create("iron-sword"));

    private readonly CampaignEvents _events = new();

    private CampaignInstances MakeInstances() => CampaignInstances.Create(_events);

    [Fact]
    public void Add_returns_an_instance_with_an_empty_patch_and_a_fresh_id()
    {
        var instances = MakeInstances();

        var created = instances.Add(Goblin, "Krzywy");

        Assert.NotEqual(default, created.Id);
        Assert.Equal(Goblin, created.Source);
        Assert.Equal("Krzywy", created.Label);
        Assert.True(created.Patch.IsEmpty);
    }

    [Fact]
    public void Two_adds_of_the_same_entry_produce_two_different_instances()
    {
        var instances = MakeInstances();

        var first = instances.Add(Goblin, null);
        var second = instances.Add(Goblin, null);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, instances.All.Count);
    }

    [Fact]
    public void Find_of_an_unknown_id_returns_null()
        => Assert.Null(MakeInstances().Find(InstanceId.New()));

    [Fact]
    public void Find_of_an_added_instance_returns_it()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, "Krzywy");

        Assert.Equal(created, instances.Find(created.Id));
    }

    [Fact]
    public void Remove_deletes_the_instance()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, null);

        instances.Remove(created.Id);

        Assert.Null(instances.Find(created.Id));
        Assert.Empty(instances.All);
    }

    [Fact]
    public void Relabel_changes_only_the_label()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, "Krzywy");
        var patch = ContentValues.From(new Dictionary<string, object> { ["hp"] = 3 });
        instances.ReplacePatch(created.Id, patch);

        instances.Relabel(created.Id, "Zguba");

        var found = instances.Find(created.Id)!;
        Assert.Equal("Zguba", found.Label);
        Assert.Same(patch, found.Patch);
    }

    [Fact]
    public void ReplacePatch_changes_only_the_patch()
    {
        var instances = MakeInstances();
        var created = instances.Add(Sword, "Zguba");
        var patch = ContentValues.From(new Dictionary<string, object> { ["quantity"] = 2 });

        instances.ReplacePatch(created.Id, patch);

        var found = instances.Find(created.Id)!;
        Assert.Equal("Zguba", found.Label);
        Assert.Same(patch, found.Patch);
    }

    [Fact]
    public void Add_publishes_exactly_one_InstanceAdded_with_the_right_id()
    {
        var instances = MakeInstances();
        var heard = new List<InstanceId>();
        _events.Subscribe<InstanceAdded>(added => heard.Add(added.Id));

        var created = instances.Add(Goblin, null);

        Assert.Equal([created.Id], heard);
    }

    [Fact]
    public void Remove_publishes_exactly_one_InstanceRemoved_with_the_right_id()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, null);
        var heard = new List<InstanceId>();
        _events.Subscribe<InstanceRemoved>(removed => heard.Add(removed.Id));

        instances.Remove(created.Id);

        Assert.Equal([created.Id], heard);
    }

    [Fact]
    public void Relabel_publishes_exactly_one_InstanceRelabelled_with_the_right_id()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, null);
        var heard = new List<InstanceId>();
        _events.Subscribe<InstanceRelabelled>(relabelled => heard.Add(relabelled.Id));

        instances.Relabel(created.Id, "Zguba");

        Assert.Equal([created.Id], heard);
    }

    [Fact]
    public void ReplacePatch_publishes_exactly_one_InstancePatchReplaced_with_the_right_id()
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, null);
        var heard = new List<InstanceId>();
        _events.Subscribe<InstancePatchReplaced>(replaced => heard.Add(replaced.Id));

        instances.ReplacePatch(created.Id, ContentValues.From(new Dictionary<string, object> { ["hp"] = 3 }));

        Assert.Equal([created.Id], heard);
    }

    [Fact]
    public void Hydrate_publishes_nothing_and_restores_the_given_instances()
    {
        var heard = 0;
        _events.Subscribe<InstanceAdded>(_ => heard++);
        var seed = new CampaignInstance(InstanceId.New(), Goblin, "Krzywy", ContentValues.Empty);

        var instances = CampaignInstances.Hydrate(_events, [seed]);

        Assert.Equal(0, heard);
        Assert.Equal(seed, instances.Find(seed.Id));
        Assert.Equal([seed], instances.All);
    }

    [Fact]
    public void Hydrate_rejects_a_repeated_instance_id()
    {
        var id = InstanceId.New();
        var first = new CampaignInstance(id, Goblin, null, ContentValues.Empty);
        var second = new CampaignInstance(id, Sword, null, ContentValues.Empty);

        Assert.Throws<ArgumentException>(() => CampaignInstances.Hydrate(_events, [first, second]));
    }

    [Fact]
    public void Remove_of_an_unknown_id_throws()
        => Assert.Throws<InvalidOperationException>(() => MakeInstances().Remove(InstanceId.New()));

    [Fact]
    public void Relabel_of_an_unknown_id_throws()
        => Assert.Throws<InvalidOperationException>(() => MakeInstances().Relabel(InstanceId.New(), "name"));

    [Fact]
    public void ReplacePatch_of_an_unknown_id_throws()
        => Assert.Throws<InvalidOperationException>(
            () => MakeInstances().ReplacePatch(InstanceId.New(), ContentValues.Empty));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_whitespace_only_label_becomes_null_on_add(string? label)
        => Assert.Null(MakeInstances().Add(Goblin, label).Label);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_whitespace_only_label_becomes_null_on_relabel(string? label)
    {
        var instances = MakeInstances();
        var created = instances.Add(Goblin, "Krzywy");

        instances.Relabel(created.Id, label);

        Assert.Null(instances.Find(created.Id)!.Label);
    }

    [Fact]
    public void A_freshly_created_campaign_has_no_instances()
    {
        var campaign = Campaign.Create(
            CampaignName.Create("Test"),
            TimeProvider.System);

        Assert.Empty(campaign.Instances.All);
    }
}
