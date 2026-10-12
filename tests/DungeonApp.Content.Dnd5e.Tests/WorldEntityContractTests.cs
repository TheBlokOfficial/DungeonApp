using System;
using System.IO;
using System.Linq;
using DungeonApp.Core.Entries;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>What the system tells the world tree: which types stand in the world, and with which icon.</summary>
public sealed class WorldEntityContractTests
{
    private static readonly Dnd5eSystem Dnd5e = new(
        [Path.Combine(Path.GetTempPath(), $"dnd5e-world-contract-packs-{Guid.NewGuid():N}")]);

    private static ContentTypeReference TypeOf(string id) => new(Dnd5e.ContentSetId, ContentId.Create(id));

    [Fact]
    public void Creatures_and_gear_stand_in_the_world_with_their_library_tab_icons_and_conditions_do_not()
    {
        var types = Dnd5e.EntityTypes.ToDictionary(type => type.Type, type => type.IconResourceKey);

        Assert.Equal(2, types.Count);
        Assert.Equal("DungeonIconSkull", types[TypeOf("creature")]);
        Assert.Equal("DungeonIconBackpack", types[TypeOf("gear")]);
        Assert.DoesNotContain(TypeOf("condition"), types.Keys);
    }

    [Fact]
    public void The_icons_match_the_system_tabs()
    {
        var tabIcons = Dnd5e.SystemTabs.Select(tab => tab.IconResourceKey).ToArray();

        Assert.All(Dnd5e.EntityTypes, type => Assert.Contains(type.IconResourceKey, tabIcons));
    }
}
