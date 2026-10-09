using System;
using System.IO;
using System.Linq;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>What the system tells the world tree: which types stand in the world, with which icon, and the hint at the end of a row.</summary>
public sealed class WorldEntityContractTests
{
    private static readonly Dnd5eSystem Dnd5e = new(
        [Path.Combine(Path.GetTempPath(), $"dnd5e-world-contract-packs-{Guid.NewGuid():N}")]);

    private static ContentTypeReference TypeOf(string id) => new(Dnd5e.ContentSetId, ContentId.Create(id));

    private static Creature SampleCreature(int hp, int? currentHp = null) => new()
    {
        Size = "Mały",
        Type = "goblinoid",
        Alignment = "chaotyczne zło",
        Combat = new CombatAspect { Ac = 15, Hp = hp, CurrentHp = currentHp, Str = 8, Dex = 14, Con = 10, Int = 10, Wis = 8, Cha = 8 },
        Speed = "9 m",
        Senses = "wzrok w ciemności 18 m",
        Challenge = "1/4",
        Actions = new StatblockSection { Entries = [new StatblockEntry { Name = "Tasak", Text = "Tnie." }] },
    };

    private static RegisteredEntry Registered(string typeId, string entryId, ContentValues values)
    {
        var reference = TypeOf(typeId);
        var entry = new Entry(ContentId.Create(entryId), entryId, reference, 1, values);

        Assert.True(Dnd5e.TryGet(reference, out var descriptor));

        return RegisteredEntry.CreateResolved(new EntryAddress(ContentId.Create("pack"), ContentId.Create(entryId)), entry, descriptor);
    }

    private static ResolvedEntity Resolve(RegisteredEntry registered, ContentValues patch)
    {
        var pack = new Pack(registered.Address.Pack, "pack", new PackVersion(1, 0), [registered.Entry]);
        var resolver = new EntityResolver(new ContentRegistry([pack], [registered], [], []), Dnd5e);

        return resolver.Resolve(new CampaignEntity
        {
            Id = EntityId.New(),
            Source = registered.Address,
            Patch = patch,
        });
    }

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

    [Fact]
    public void A_creature_shows_its_maximum_health_when_none_was_lost_and_the_current_value_otherwise()
    {
        var registered = Registered("creature", "goblin", ContentValues.From(SampleCreature(7)));

        Assert.Equal("7/7", Dnd5e.RowHint(Resolve(registered, ContentValues.Empty)));

        var wounded = ContentValues.From(SampleCreature(7, currentHp: 3));
        Assert.Equal("3/7", Dnd5e.RowHint(Resolve(registered, wounded)));
    }

    [Fact]
    public void Gear_and_an_unresolved_entity_have_no_hint()
    {
        var gear = Registered(
            "gear", "potion", ContentValues.From(new Gear { Rarity = "Pospolity", Category = "Mikstura", Item = new ItemAspect { Weight = 0.25m, Value = 50 } }));
        Assert.Null(Dnd5e.RowHint(Resolve(gear, ContentValues.Empty)));

        var missing = new EntityResolver(new ContentRegistry([], [], [], []), Dnd5e).Resolve(new CampaignEntity
        {
            Id = EntityId.New(),
            Source = new EntryAddress(ContentId.Create("gone"), ContentId.Create("goblin")),
            Patch = ContentValues.Empty,
        });
        Assert.Null(Dnd5e.RowHint(missing));
    }
}
