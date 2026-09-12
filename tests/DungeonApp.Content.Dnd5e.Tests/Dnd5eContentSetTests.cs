using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Deliberately minimal, per the brief for this pass: four tests, proving exactly the two things
/// this project's shape depends on. The first two prove a real fixture entry deserializes into the
/// right record with the right values; the last two are the ones that matter most - they prove the
/// deserializer (<c>System.Text.Json</c>, driven by <see cref="Monster"/>/<see cref="Gear"/>'s own
/// <see langword="required"/> members and strict unmapped-member handling) is what rejects bad
/// content now, with zero bespoke validation code written anywhere in
/// <see cref="Dnd5eContentSet.TryValidate"/>.
/// </summary>
public sealed class Dnd5eContentSetTests
{
    [Fact]
    public async Task Goblin_entry_deserializes_into_a_monster_with_correct_values()
    {
        var registry = await LoadFixturesAsync();

        var goblin = registry.Entries.Single(entry => entry.Address.Entry.Value == "goblin");
        Assert.Null(goblin.Unresolved);

        var monster = goblin.Entry.Values.Read<Monster>();

        Assert.Equal("Mały", monster.Size);
        Assert.Equal("humanoid (goblinoid)", monster.Type);
        Assert.Equal("neutralny zły", monster.Alignment);
        Assert.Equal(15, monster.Ac);
        Assert.Equal("zbroja skórzana, tarcza", monster.AcSource);
        Assert.Equal(7, monster.Hp);
        Assert.Equal("30 stóp", monster.Speed);
        Assert.Equal(8, monster.Str);
        Assert.Equal(14, monster.Dex);
        Assert.Equal("1/4 (50 PD)", monster.Challenge);
        Assert.Contains("Zwinna ucieczka", monster.SpecialAbilities);
    }

    [Fact]
    public async Task Healing_potion_entry_deserializes_into_a_gear_with_correct_values()
    {
        var registry = await LoadFixturesAsync();

        var potion = registry.Entries.Single(entry => entry.Address.Entry.Value == "healing-potion");
        Assert.Null(potion.Unresolved);

        var gear = potion.Entry.Values.Read<Gear>();

        Assert.Equal("Pospolity", gear.Rarity);
        Assert.Equal(1, gear.Weight);
        Assert.Contains("2k4+2", gear.Description);
    }

    [Fact]
    public async Task An_unknown_key_in_values_is_rejected()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", EntryJsonWithUnknownKey);

        var registry = await new ContentPackLoader(packs.Path, new Dnd5eContentSet()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains("bogusField", entry.UnresolvedDetail);
    }

    [Fact]
    public async Task A_missing_required_value_is_rejected()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", EntryJsonMissingRequiredActions);

        var registry = await new ContentPackLoader(packs.Path, new Dnd5eContentSet()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains("actions", entry.UnresolvedDetail, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<ContentRegistry> LoadFixturesAsync() =>
        new ContentPackLoader(RepositoryRoot.PackFixtures, new Dnd5eContentSet()).LoadAsync(CancellationToken.None);

    private const string PackJson = """
        {
          "formatVersion": 1,
          "id": "pack",
          "name": "Pack",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    private const string EntryJsonWithUnknownKey = """
        {
          "id": "e1",
          "name": "Test Monster",
          "template": "dnd5e:monster",
          "templateVersion": 1,
          "values": {
            "size": "Mały",
            "type": "humanoid",
            "alignment": "neutralny",
            "ac": 10,
            "hp": 5,
            "speed": "30 stóp",
            "str": 10,
            "dex": 10,
            "con": 10,
            "int": 10,
            "wis": 10,
            "cha": 10,
            "senses": "zwykły wzrok",
            "challenge": "0",
            "actions": "Brak.",
            "bogusField": "x"
          }
        }
        """;

    private const string EntryJsonMissingRequiredActions = """
        {
          "id": "e1",
          "name": "Test Monster",
          "template": "dnd5e:monster",
          "templateVersion": 1,
          "values": {
            "size": "Mały",
            "type": "humanoid",
            "alignment": "neutralny",
            "ac": 10,
            "hp": 5,
            "speed": "30 stóp",
            "str": 10,
            "dex": 10,
            "con": 10,
            "int": 10,
            "wis": 10,
            "cha": 10,
            "senses": "zwykły wzrok",
            "challenge": "0"
          }
        }
        """;
}
