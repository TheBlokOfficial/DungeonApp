using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Entries;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Testing;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Deliberately minimal, per the brief for this pass: four tests, proving exactly the two things
/// this project's shape depends on. The first two prove a real fixture entry deserializes into the
/// right record with the right values; the last two are the ones that matter most - they prove the
/// deserializer (<c>System.Text.Json</c>, driven by <see cref="Monster"/>/<see cref="Gear"/>'s own
/// <see langword="required"/> members and strict unmapped-member handling) is what rejects bad
/// content now, with zero bespoke validation code written anywhere in
/// <see cref="Dnd5eSystem.TryValidate"/>. The picture tests at the end (porcja 4a) prove the one
/// thing this system adds to the loader's own picture-path check: that both types declare it.
/// </summary>
public sealed class Dnd5eSystemTests
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

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

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

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains("actions", entry.UnresolvedDetail, StringComparison.OrdinalIgnoreCase);
    }

    // Porcja 4a: both types declare their "image" value as the entry's picture, so the loader checks
    // its path - a picture reaching outside the pack rejects the entry, a picture in a subdirectory
    // of the pack reads back as written.
    [Theory]
    [InlineData("monster")]
    [InlineData("gear")]
    public void Both_content_types_declare_image_as_their_picture(string type)
    {
        var reference = new ContentTypeReference(ContentId.Create("dnd5e"), ContentId.Create(type));

        Assert.True(NewSystem().TryGet(reference, out var descriptor));
        Assert.Equal("image", descriptor.ImageProperty);
    }

    [Theory]
    [InlineData("../poza.png", false)]
    [InlineData("obrazy/mikstura.webp", true)]
    public async Task A_gear_picture_path_is_checked_when_its_pack_loads(string image, bool resolves)
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", $$"""
            {
              "id": "potion",
              "name": "Mikstura",
              "template": "dnd5e:gear",
              "templateVersion": 1,
              "values": { "rarity": "Pospolity", "image": "{{image}}" }
            }
            """);

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(resolves, entry.Unresolved is null);
        Assert.Equal(image, entry.Entry.Values.Read<Gear>().Image);
    }

    private static Task<ContentRegistry> LoadFixturesAsync() =>
        new ContentPackLoader(RepositoryRoot.PackFixtures, NewSystem()).LoadAsync(CancellationToken.None);

    /// <summary>
    /// None of these tests ever open a campaign, so nothing here writes to the layout store - a
    /// fresh temp directory per call is enough, the same isolation pattern
    /// <c>WorkspaceLayoutStoreTests</c> uses for the real thing.
    /// </summary>
    private static Dnd5eSystem NewSystem() => new(
        new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"dnd5e-system-tests-{Guid.NewGuid():N}")),
        [Path.Combine(Path.GetTempPath(), $"dnd5e-system-tests-packs-{Guid.NewGuid():N}")]);

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
