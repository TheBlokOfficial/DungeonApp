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
/// The first two tests prove a real fixture entry deserializes into the right record with the right
/// values; the next two are the ones that matter most - they prove the deserializer
/// (<c>System.Text.Json</c>, driven by <see cref="Creature"/>/<see cref="Gear"/>'s own
/// <see langword="required"/> members, their aspects' and strict unmapped-member handling) is what
/// rejects bad content, with zero bespoke validation code written anywhere in
/// <see cref="Dnd5eSystem.TryValidate"/>. The picture tests prove the one thing this system adds to
/// the loader's own picture-path check: that both types declare it.
/// </summary>
public sealed class Dnd5eSystemTests
{
    [Fact]
    public async Task Goblin_entry_deserializes_into_a_creature_with_correct_values()
    {
        var registry = await LoadFixturesAsync();

        var goblin = registry.Entries.Single(entry => entry.Address.Entry.Value == "goblin");
        Assert.Null(goblin.Unresolved);

        var creature = goblin.Entry.Values.Read<Creature>();

        Assert.Equal("Mały", creature.Size);
        Assert.Equal("humanoid (goblinoid)", creature.Type);
        Assert.Equal("neutralny zły", creature.Alignment);
        Assert.Equal(15, creature.Combat!.Ac);
        Assert.Equal("zbroja skórzana, tarcza", creature.Combat.AcSource);
        Assert.Equal(7, creature.Combat.Hp);
        Assert.Equal("2k6", creature.Combat.HpDice);
        Assert.Null(creature.Combat.CurrentHp);
        Assert.Equal("30 stóp", creature.Speed);
        Assert.Equal(8, creature.Combat.Str);
        Assert.Equal(14, creature.Combat.Dex);
        Assert.Equal(8, creature.Combat.Cha);
        Assert.Equal("1/4 (50 PD)", creature.Challenge);
        Assert.Equal("Zwinna ucieczka", Assert.Single(creature.SpecialAbilities!.Entries).Name);
        Assert.Equal(["Bułat", "Krótki łuk"], creature.Actions!.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task Every_optional_creature_field_is_read_under_its_own_name()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", EntryJsonWithEveryOptionalCreatureField);

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Null(entry.UnresolvedDetail);
        var creature = entry.Entry.Values.Read<Creature>();

        Assert.Equal("Kon +4, Mdr +2", creature.SavingThrows);
        Assert.Equal("obuchowe", creature.DamageVulnerabilities);
        Assert.Equal("od zimna", creature.DamageResistances);
        Assert.Equal("od trucizn", creature.DamageImmunities);
        Assert.Equal("zatrucie", creature.ConditionImmunities);
        Assert.Equal(1800, creature.Xp);
        Assert.Equal("Wrodzone rzucanie czarów.", creature.Spellcasting!.Intro);
        Assert.Equal("światło", Assert.Single(creature.Spellcasting.Entries).Text);
        Assert.Equal("Odskok", Assert.Single(creature.BonusActions!.Entries).Name);
        Assert.Equal("Parowanie", Assert.Single(creature.Reactions!.Entries).Name);
        var legendary = Assert.Single(creature.LegendaryActions!.Entries);
        Assert.Equal("Kopyta", legendary.Name);
        Assert.Equal("kosztuje 2 akcje", legendary.Note);
        Assert.Equal("pancerz naturalny", creature.Combat!.AcSource);
        Assert.Equal("9k10+18", creature.Combat.HpDice);
        Assert.Equal(4, creature.Combat.CurrentHp);
    }

    /// <summary>
    /// The combat aspect carries its own required values; a creature needs the aspect, actions and a
    /// challenge, and an actions section without an entry is refused like a missing one. Each reason
    /// names the key.
    /// </summary>
    [Theory]
    [InlineData("\"challenge\": \"0\", \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "combat")]
    [InlineData("\"combat\": { \"ac\": 10, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10 }, \"challenge\": \"0\", \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "hp")]
    [InlineData("\"combat\": { \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10 }, \"challenge\": \"0\", \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "ac")]
    [InlineData("\"combat\": { \"ac\": 10, \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10 }, \"challenge\": \"0\", \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "cha")]
    [InlineData("\"combat\": { \"ac\": 10, \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10, \"initiative\": 2 }, \"challenge\": \"0\", \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "initiative")]
    [InlineData("\"combat\": { \"ac\": 10, \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10 }, \"challenge\": \"0\"", "actions")]
    [InlineData("\"combat\": { \"ac\": 10, \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10 }, \"actions\": { \"entries\": [{ \"name\": \"A\", \"text\": \"B\" }] }", "challenge")]
    [InlineData("\"combat\": { \"ac\": 10, \"hp\": 5, \"str\": 10, \"dex\": 10, \"con\": 10, \"int\": 10, \"wis\": 10, \"cha\": 10 }, \"challenge\": \"0\", \"actions\": { \"intro\": \"Nic.\", \"entries\": [] }", "actions")]
    public async Task A_malformed_creature_entry_is_rejected(string values, string named)
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", CreatureEntryJson(
            "\"size\": \"Mały\", \"type\": \"humanoid\", \"alignment\": \"neutralny\", \"speed\": \"9 m\", \"senses\": \"zwykły wzrok\", " + values));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains(named, entry.UnresolvedDetail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The flat shape the type had before its aspects - armor class, hit points and the scores
    /// beside the type's own fields - is not read back: the first of those keys rejects the entry.
    /// </summary>
    [Fact]
    public async Task A_creature_in_the_flat_shape_is_rejected()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", CreatureEntryJson("""
            "size": "Mały", "type": "humanoid", "alignment": "neutralny", "speed": "9 m", "senses": "zwykły wzrok",
            "ac": 10, "hp": 5, "str": 10, "dex": 10, "con": 10, "int": 10, "wis": 10, "cha": 10,
            "challenge": "0", "actions": { "entries": [{ "name": "A", "text": "B" }] }
            """));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains("ac", entry.UnresolvedDetail, StringComparison.Ordinal);
    }

    /// <summary>
    /// The type's old name is not this system's type any more: such an entry is marked as of a
    /// missing type - the content tab tells the GM which type it is - and never read.
    /// </summary>
    [Fact]
    public async Task An_entry_of_the_old_monster_type_is_of_a_type_the_system_does_not_know()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", EntryJsonWithEveryOptionalCreatureField.Replace(
            "dnd5e:creature", "dnd5e:monster", StringComparison.Ordinal));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingType, entry.Unresolved);
        Assert.Equal("monster", entry.Entry.Type.Type.Value);
    }

    private static string CreatureEntryJson(string values) => $$"""
        {
          "id": "c1",
          "name": "Test Creature",
          "template": "dnd5e:creature",
          "templateVersion": 1,
          "values": { {{values}} }
        }
        """;

    [Fact]
    public async Task Healing_potion_entry_deserializes_into_a_gear_with_correct_values()
    {
        var registry = await LoadFixturesAsync();

        var potion = registry.Entries.Single(entry => entry.Address.Entry.Value == "healing-potion");
        Assert.Null(potion.Unresolved);

        var gear = potion.Entry.Values.Read<Gear>();

        Assert.Equal("Pospolity", gear.Rarity);
        Assert.True(gear.Magical);
        Assert.Equal("Mikstura", gear.Category);
        Assert.Equal(0.25m, gear.Item.Weight);
        Assert.Equal(50m, gear.Item.Value);
        Assert.Null(gear.Charges);
        Assert.False(gear.Attunement);
        Assert.Contains("2k4+2", gear.Description);
    }

    [Fact]
    public async Task Every_optional_gear_field_is_read_under_its_own_name()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", GearEntryJson("""
            "rarity": "Rzadki",
            "magical": true,
            "category": "Laska",
            "subtype": "kostur",
            "attunement": true,
            "attunementBy": "przez barda, kapłana albo druida",
            "item": { "weight": 2, "value": 0.5 },
            "damage": "1k6",
            "damageType": "obuchowe",
            "properties": "półtoraręczna (1k8)",
            "armorClass": "11",
            "armorClassNote": "+ mod. Zr",
            "strengthRequirement": 13,
            "stealthDisadvantage": true,
            "charges": { "max": 10, "recharge": "odzyskuje 1k6+4 ładunków o świcie" },
            "description": "Gładki kij."
            """));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Null(entry.UnresolvedDetail);
        var gear = entry.Entry.Values.Read<Gear>();

        Assert.True(gear.Magical);
        Assert.Equal("kostur", gear.Subtype);
        Assert.True(gear.Attunement);
        Assert.Equal("przez barda, kapłana albo druida", gear.AttunementBy);
        Assert.Equal(2m, gear.Item.Weight);
        Assert.Equal(0.5m, gear.Item.Value);
        Assert.Equal("1k6", gear.Damage);
        Assert.Equal("obuchowe", gear.DamageType);
        Assert.Equal("półtoraręczna (1k8)", gear.Properties);
        Assert.Equal("11", gear.ArmorClass);
        Assert.Equal("+ mod. Zr", gear.ArmorClassNote);
        Assert.Equal(13, gear.StrengthRequirement);
        Assert.True(gear.StealthDisadvantage);
        Assert.Equal(10, gear.Charges!.Max);
        Assert.Equal("odzyskuje 1k6+4 ładunków o świcie", gear.Charges.Recharge);
    }

    // The gear record and its aspects are their own validator, like the creature's: a missing
    // required value (the item aspect, its weight and worth, the charges' maximum included), an
    // unknown key (the flat shape's weight and charges among them), a who-attunes without
    // attunement, a note without the value it stands under and a negative worth or weight each
    // reject the entry, whose reason names the key.
    [Theory]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": 1, \"value\": 1 }, \"damageType\": \"cięte\"", "damageType")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Zbroja\", \"item\": { \"weight\": 1, \"value\": 1 }, \"armorClassNote\": \"+ mod. Zr\"", "armorClassNote")]
    [InlineData("\"rarity\": \"Pospolity\", \"item\": { \"weight\": 1, \"value\": 1 }", "category")]
    [InlineData("\"category\": \"Broń\", \"item\": { \"weight\": 1, \"value\": 1 }", "rarity")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"value\": 1 }", "weight")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": 1 }", "value")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\"", "item")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"weight\": 1, \"value\": 1", "weight")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Różdżka\", \"item\": { \"weight\": 1, \"value\": 1 }, \"charges\": 7", "charges")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Różdżka\", \"item\": { \"weight\": 1, \"value\": 1 }, \"charges\": { \"recharge\": \"o świcie\" }", "max")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Różdżka\", \"item\": { \"weight\": 1, \"value\": 1 }, \"recharge\": \"o świcie\"", "recharge")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": 1, \"value\": 1 }, \"price\": 15", "price")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": 1, \"value\": 1 }, \"attunementBy\": \"przez maga\"", "attunement")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": 1, \"value\": -1 }", "value")]
    [InlineData("\"rarity\": \"Pospolity\", \"category\": \"Broń\", \"item\": { \"weight\": -1, \"value\": 1 }", "weight")]
    public async Task A_malformed_gear_entry_is_rejected(string values, string named)
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", GearEntryJson(values));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
        Assert.Contains(named, entry.UnresolvedDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_item_with_only_its_required_values_is_read_with_nothing_else_set()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", GearEntryJson("\"rarity\": \"Pospolity\", \"category\": \"Ekwipunek\", \"item\": { \"weight\": 0, \"value\": 0 }"));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var gear = Assert.Single(registry.Entries).Entry.Values.Read<Gear>();
        Assert.Equal(new Gear { Rarity = "Pospolity", Category = "Ekwipunek", Item = new ItemAspect { Weight = 0, Value = 0 } }, gear);
    }

    private static string GearEntryJson(string values) => $$"""
        {
          "id": "g1",
          "name": "Test Item",
          "template": "dnd5e:gear",
          "templateVersion": 1,
          "values": { {{values}} }
        }
        """;

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

    // A section is a structure, not prose: plain text where a section belongs, an entry without a
    // name or text, and a section saying nothing are each refused, so the pack author sees the entry
    // rejected instead of a card drawn with holes in it.
    [Theory]
    [InlineData("\"Ugryzienie.\"")]
    [InlineData("""{ "entries": [{ "name": "Ugryzienie" }] }""")]
    [InlineData("""{ "entries": [{ "text": "Gryzie." }] }""")]
    [InlineData("""{ "entries": [{ "name": " ", "text": "Gryzie." }] }""")]
    [InlineData("""{ "entries": [] }""")]
    public async Task A_malformed_section_is_rejected(string actions)
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", EntryJsonMissingRequiredActions.Replace(
            "\"challenge\": \"0\"",
            $"\"challenge\": \"0\", \"actions\": {actions}",
            StringComparison.Ordinal));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, entry.Unresolved);
    }

    // Both types declare their "image" value as the entry's picture, so the loader checks
    // its path - a picture reaching outside the pack rejects the entry, a picture in a subdirectory
    // of the pack reads back as written.
    [Theory]
    [InlineData("creature")]
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
              "values": { "rarity": "Pospolity", "category": "Mikstura", "item": { "weight": 0.25, "value": 50 }, "image": "{{image}}" }
            }
            """);

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(resolves, entry.Unresolved is null);
        Assert.Equal(image, entry.Entry.Values.Read<Gear>().Image);
    }

    [Fact]
    public void A_condition_declares_its_icon_as_its_picture()
    {
        var reference = new ContentTypeReference(ContentId.Create("dnd5e"), ContentId.Create("condition"));

        Assert.True(NewSystem().TryGet(reference, out var descriptor));
        Assert.Equal("Stan", descriptor.Name);
        Assert.Equal(1, descriptor.Version);
        Assert.Equal("icon", descriptor.ImageProperty);
    }

    [Fact]
    public async Task A_condition_entry_deserializes_with_its_summary_icon_and_rules()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", ConditionEntryJson("""
            "summary": "Leży na ziemi.",
            "icon": "ikony/powalony.png",
            "rules": {
              "intro": "Istota ma **utrudnienie** w rzutach ataku.",
              "entries": [{ "name": "Poziom 1", "text": "Utrudnienie w testach cech." }]
            }
            """));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Null(entry.UnresolvedDetail);
        var condition = entry.Entry.Values.Read<StatusCondition>();

        Assert.Equal("Leży na ziemi.", condition.Summary);
        Assert.Equal("ikony/powalony.png", condition.Icon);
        Assert.Equal("Istota ma **utrudnienie** w rzutach ataku.", condition.Rules.Intro);
        Assert.Equal("Poziom 1", Assert.Single(condition.Rules.Entries).Name);
    }

    [Fact]
    public async Task A_condition_without_an_icon_is_read_with_none()
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", ConditionEntryJson("""
            "summary": "Leży na ziemi.", "rules": { "intro": "Czołga się.", "entries": [] }
            """));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.Null(entry.UnresolvedDetail);
        Assert.Null(entry.Entry.Values.Read<StatusCondition>().Icon);
    }

    // The condition record is its own validator: a missing or blank summary, missing rules, rules
    // saying nothing, rules written as plain text, an unknown key and an icon reaching outside the
    // pack each reject the entry; the reason names the key where there is one.
    [Theory]
    [InlineData("\"rules\": { \"intro\": \"Czołga się.\", \"entries\": [] }", "summary")]
    [InlineData("\"summary\": \" \", \"rules\": { \"intro\": \"Czołga się.\", \"entries\": [] }", "summary")]
    [InlineData("\"summary\": \"Leży.\"", "rules")]
    [InlineData("\"summary\": \"Leży.\", \"rules\": { \"entries\": [] }", "intro")]
    [InlineData("\"summary\": \"Leży.\", \"rules\": \"Czołga się.\"", "rules")]
    [InlineData("\"summary\": \"Leży.\", \"rules\": { \"intro\": \"Czołga się.\", \"entries\": [] }, \"image\": \"a.png\"", "image")]
    [InlineData("\"summary\": \"Leży.\", \"rules\": { \"intro\": \"Czołga się.\", \"entries\": [] }, \"icon\": \"../poza.png\"", "")]
    public async Task A_malformed_condition_entry_is_rejected(string values, string named)
    {
        using var packs = new TemporaryPacks();
        packs.WriteFile("pack", "pack.json", PackJson);
        packs.WriteFile("pack", "entries/e.json", ConditionEntryJson(values));

        var registry = await new ContentPackLoader(packs.Path, NewSystem()).LoadAsync();

        var entry = Assert.Single(registry.Entries);
        Assert.NotNull(entry.Unresolved);
        Assert.Contains(named, entry.UnresolvedDetail, StringComparison.OrdinalIgnoreCase);
    }

    private static string ConditionEntryJson(string values) => $$"""
        {
          "id": "s1",
          "name": "Powalony",
          "template": "dnd5e:condition",
          "templateVersion": 1,
          "values": { {{values}} }
        }
        """;

    [Fact]
    public void A_content_type_the_system_does_not_declare_is_neither_found_nor_validated()
    {
        var system = NewSystem();
        var unknown = new ContentTypeReference(ContentId.Create("dnd5e"), ContentId.Create("spell"));

        Assert.False(system.TryGet(unknown, out _));
        Assert.False(system.TryValidate(unknown, ContentValues.Empty, out var error));
        Assert.Contains("spell", error);
    }

    [Fact]
    public void A_content_type_from_another_set_is_not_the_systems_own()
    {
        var system = NewSystem();
        var foreign = new ContentTypeReference(ContentId.Create("other"), ContentId.Create("creature"));

        Assert.False(system.HasSet(foreign.Set));
        Assert.False(system.TryGet(foreign, out _));
        Assert.False(system.TryValidate(foreign, ContentValues.Empty, out var error));
        Assert.Contains("does not own", error);
    }

    [Fact]
    public void Each_system_tab_is_built_from_the_content_tab_definition_it_is_named_after()
    {
        var system = NewSystem();

        Assert.Equal(
            system.ContentTabDefinitions.Select(definition => definition.Title),
            system.SystemTabs.Select(tab => tab.Title));
    }

    private static Task<ContentRegistry> LoadFixturesAsync() =>
        new ContentPackLoader(RepositoryRoot.PackFixtures, NewSystem()).LoadAsync(CancellationToken.None);

    private static Dnd5eSystem NewSystem() => new(
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
          "name": "Test Creature",
          "template": "dnd5e:creature",
          "templateVersion": 1,
          "values": {
            "size": "Mały",
            "type": "humanoid",
            "alignment": "neutralny",
            "combat": { "ac": 10, "hp": 5, "str": 10, "dex": 10, "con": 10, "int": 10, "wis": 10, "cha": 10 },
            "speed": "30 stóp",
            "senses": "zwykły wzrok",
            "challenge": "0",
            "actions": { "entries": [{ "name": "Brak", "text": "Nic nie robi." }] },
            "bogusField": "x"
          }
        }
        """;

    private const string EntryJsonWithEveryOptionalCreatureField = """
        {
          "id": "e1",
          "name": "Test Creature",
          "template": "dnd5e:creature",
          "templateVersion": 1,
          "values": {
            "size": "Duży",
            "type": "niebianin",
            "alignment": "praworządny dobry",
            "combat": { "ac": 12, "acSource": "pancerz naturalny", "hp": 67, "hpDice": "9k10+18", "currentHp": 4, "str": 18, "dex": 14, "con": 15, "int": 11, "wis": 17, "cha": 16 },
            "speed": "15 m",
            "savingThrows": "Kon +4, Mdr +2",
            "damageVulnerabilities": "obuchowe",
            "damageResistances": "od zimna",
            "damageImmunities": "od trucizn",
            "conditionImmunities": "zatrucie",
            "senses": "pasywna Percepcja 13",
            "challenge": "5",
            "xp": 1800,
            "actions": { "entries": [{ "name": "Róg", "text": "**+7 do trafienia**." }] },
            "spellcasting": { "intro": "Wrodzone rzucanie czarów.", "entries": [{ "name": "Bez ograniczeń", "text": "światło" }] },
            "bonusActions": { "entries": [{ "name": "Odskok", "text": "Odskakuje." }] },
            "reactions": { "entries": [{ "name": "Parowanie", "text": "Paruje." }] },
            "legendaryActions": { "entries": [{ "name": "Kopyta", "note": "kosztuje 2 akcje", "text": "Kopie." }] }
          }
        }
        """;

    private const string EntryJsonMissingRequiredActions = """
        {
          "id": "e1",
          "name": "Test Creature",
          "template": "dnd5e:creature",
          "templateVersion": 1,
          "values": {
            "size": "Mały",
            "type": "humanoid",
            "alignment": "neutralny",
            "combat": { "ac": 10, "hp": 5, "str": 10, "dex": 10, "con": 10, "int": 10, "wis": 10, "cha": 10 },
            "speed": "30 stóp",
            "senses": "zwykły wzrok",
            "challenge": "0"
          }
        }
        """;
}
