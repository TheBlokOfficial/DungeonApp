using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Content;

public sealed class ContentPackLoaderTests : IDisposable
{
    private readonly TemporaryPacks _packs = new();

    /// <summary>A template with one required text field, one required integer field, and one optional text field.</summary>
    private const string ValidTemplateJson = """
        {
          "id": "thing",
          "name": "Thing",
          "version": 1,
          "catalogVersion": 1,
          "fields": [
            { "id": "label", "label": "Label", "type": "text" },
            { "id": "amount", "label": "Amount", "type": "integer" },
            { "id": "note", "label": "Note", "type": "text", "required": false }
          ],
          "card": [
            { "element": "statblock", "traits": [ { "field": "label" }, { "field": "amount" } ] },
            { "element": "prose", "field": "note" }
          ]
        }
        """;

    private const string ValidEntryJson = """
        {
          "id": "e1",
          "name": "Entry One",
          "template": "sys:thing",
          "templateVersion": 1,
          "values": { "label": "A", "amount": 3 }
        }
        """;

    public void Dispose() => _packs.Dispose();

    private ContentPackLoader Loader() => new(_packs.Path);

    private static string SystemPackJson(string id, string name = "System", int major = 1, int minor = 0, int formatVersion = 1) =>
        $$"""
        {
          "formatVersion": {{formatVersion}},
          "id": "{{id}}",
          "kind": "system",
          "name": "{{name}}",
          "version": { "major": {{major}}, "minor": {{minor}} }
        }
        """;

    private static string ContentPackJson(string id, string name = "Content", int major = 1, int minor = 0, int formatVersion = 1) =>
        $$"""
        {
          "formatVersion": {{formatVersion}},
          "id": "{{id}}",
          "kind": "content",
          "name": "{{name}}",
          "version": { "major": {{major}}, "minor": {{minor}} }
        }
        """;

    // ---------------------------------------------------------------------
    // Fixture acceptance test - the hand-written packs are the format spec.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Loads_the_fixture_packs_cleanly()
    {
        var registry = await new ContentPackLoader(RepositoryRoot.PackFixtures).LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.SystemPacks);
        Assert.Single(registry.ContentPacks);

        var registered = Assert.Single(registry.Entries);
        Assert.Null(registered.Unresolved);
        Assert.NotNull(registered.Template);
        var template = registered.Template!;

        Assert.Equal("goblinoidy", registered.Address.Pack.Value);
        Assert.Equal("goblin", registered.Address.Entry.Value);
        Assert.Equal("dnd5e", registered.Entry.Template.Pack.Value);
        Assert.Equal("potwor", registered.Entry.Template.Template.Value);
        Assert.Equal("potwor", template.Id.Value);

        var values = registered.Entry.Values;
        var kp = Assert.IsType<IntegerValue>(values[FieldName.Create("kp")]);
        Assert.Equal(15L, kp.Value);
        Assert.IsType<TextValue>(values[FieldName.Create("wyzwanie")]);

        Assert.Equal(7, template.Card.Count);
        Assert.IsType<StatblockElement>(template.Card[0]);
        Assert.IsType<StatblockElement>(template.Card[1]);
        Assert.IsType<StatblockElement>(template.Card[2]);
        Assert.IsType<StatblockElement>(template.Card[3]);
        Assert.IsType<ProseElement>(template.Card[4]);
        Assert.IsType<ProseElement>(template.Card[5]);
        Assert.IsType<ProseElement>(template.Card[6]);

        var defenseAndSpeed = (StatblockElement)template.Card[1];
        var kpTrait = defenseAndSpeed.Traits.Single(trait => trait.Field == FieldName.Create("kp"));
        Assert.Equal(FieldName.Create("kpZrodlo"), kpTrait.Secondary);
    }

    // ---------------------------------------------------------------------
    // Rule 1: missing pack.json, invalid JSON, unknown formatVersion.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_pack_directory_with_no_pack_json()
    {
        _packs.WriteFile("bad", "readme.txt", "not a pack");

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("pack.json", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_json_that_is_not_valid_json()
    {
        _packs.WriteFile("bad", "pack.json", "{ this is not json");

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("pack.json", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_unknown_format_version()
    {
        _packs.WritePack("bad", SystemPackJson("sys", formatVersion: 99));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("formatVersion", rejected.Reason);
        Assert.Contains("99", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 2: invalid id, missing name, unknown kind, missing/invalid version.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_an_invalid_pack_id()
    {
        _packs.WritePack("bad", SystemPackJson("Not Valid!"));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("Not Valid!", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_with_no_name()
    {
        const string json = """
            {
              "formatVersion": 1,
              "id": "sys",
              "kind": "system",
              "version": { "major": 1, "minor": 0 }
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("name", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_unknown_pack_kind()
    {
        const string json = """
            {
              "formatVersion": 1,
              "id": "sys",
              "kind": "weird",
              "name": "Sys",
              "version": { "major": 1, "minor": 0 }
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("weird", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_with_no_version()
    {
        const string json = """
            {
              "formatVersion": 1,
              "id": "sys",
              "kind": "system",
              "name": "Sys"
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("version", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 3: a pack brings templates or entries, never both kinds of directory.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_system_pack_that_has_an_entries_directory()
    {
        _packs.WritePack("bad", SystemPackJson("sys"), new Dictionary<string, string>
        {
            ["entries/e.json"] = ValidEntryJson
        });

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("entries", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_content_pack_that_has_a_templates_directory()
    {
        _packs.WritePack("bad", ContentPackJson("cnt"), new Dictionary<string, string>
        {
            ["templates/t.json"] = ValidTemplateJson
        });

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("templates", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 4: a duplicated id inside one pack.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_duplicate_template_id_within_a_pack()
    {
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/a.json", ValidTemplateJson);
        _packs.WriteFile("sys", "templates/b.json", ValidTemplateJson);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("thing", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_duplicate_entry_id_within_a_pack()
    {
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/a.json", ValidEntryJson);
        _packs.WriteFile("cnt", "entries/b.json", ValidEntryJson);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("e1", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 5: template validation - element names, unknown keys, field rules, dangling references.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_template_card_element_of_unknown_type()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "text" } ],
              "card": [ { "element": "chart", "field": "label" } ]
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("chart", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_unknown_key_at_the_top_level_of_a_template()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "extra": true,
              "fields": [ { "id": "label", "label": "Label", "type": "text" } ],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("t.json", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_unknown_key_inside_a_field_declaration()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "text", "unit": "cm" } ],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("t.json", rejected.Reason);
    }

    /// <summary>
    /// A "prose"-only key ("field") smuggled onto a statblock element must be caught even though
    /// both variants are legitimate keys somewhere in the catalog - the strictness is per element
    /// type, not a union of every element's keys.
    /// </summary>
    [Fact]
    public async Task Rejects_a_statblock_element_carrying_a_prose_only_key()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "text" } ],
              "card": [ { "element": "statblock", "traits": [ { "field": "label" } ], "field": "label" } ]
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("t.json", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_duplicate_field_id_within_a_template()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [
                { "id": "label", "label": "Label", "type": "text" },
                { "id": "label", "label": "Label Again", "type": "text" }
              ],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("label", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_field_of_unknown_type()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "decimal" } ],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("decimal", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_card_element_referencing_an_undeclared_field()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "text" } ],
              "card": [ { "element": "prose", "field": "missing" } ]
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("missing", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_trait_secondary_referencing_an_undeclared_field()
    {
        const string json = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 1,
              "catalogVersion": 1,
              "fields": [ { "id": "label", "label": "Label", "type": "text" } ],
              "card": [ { "element": "statblock", "traits": [ { "field": "label", "secondary": "missing" } ] } ]
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("missing", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 6: entry validation - unknown key, invalid template reference.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_an_unknown_key_in_an_entry_file()
    {
        const string json = """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "sys:thing",
              "templateVersion": 1,
              "extra": true,
              "values": { }
            }
            """;
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("e.json", rejected.Reason);
    }

    [Theory]
    [InlineData("systhing")]
    [InlineData("sys:sub:thing")]
    [InlineData("Sys:thing")]
    public async Task Rejects_an_invalid_template_reference(string templateReference)
    {
        var json = $$"""
            {
              "id": "e1",
              "name": "Entry One",
              "template": "{{templateReference}}",
              "templateVersion": 1,
              "values": { }
            }
            """;
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains(templateReference, rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Rule 7: two installed packs with the same id are both rejected.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_both_packs_when_two_share_an_id()
    {
        _packs.WritePack("first", SystemPackJson("dup"));
        _packs.WritePack("second", SystemPackJson("dup"));

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.SystemPacks);
        Assert.Equal(2, registry.RejectedPacks.Count);
        Assert.All(registry.RejectedPacks, rejected => Assert.Contains("dup", rejected.Reason));
    }

    // ---------------------------------------------------------------------
    // Rule 8: size and count limits.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_pack_json_over_the_size_limit()
    {
        var oversizedName = new string('a', 1024 * 1024);
        _packs.WritePack("bad", SystemPackJson("sys", name: oversizedName));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("pack.json", rejected.Reason);
        Assert.Contains("size limit", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_system_pack_with_more_than_ten_thousand_templates()
    {
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));

        for (var i = 0; i < 10_001; i++)
        {
            _packs.WriteFile("sys", $"templates/t{i}.json", "{}");
        }

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("10000", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // The forbidden 3 -> 3 dependency edge (section 11).
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_content_pack_whose_entry_points_at_a_template_in_another_content_pack()
    {
        _packs.WriteFile("cnt-a", "pack.json", ContentPackJson("cnt-a"));
        _packs.WriteFile("cnt-a", "entries/e.json", """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "cnt-b:thing",
              "templateVersion": 1,
              "values": { }
            }
            """);
        _packs.WriteFile("cnt-b", "pack.json", ContentPackJson("cnt-b"));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("cnt-b", rejected.Reason);
        Assert.Single(registry.ContentPacks, pack => pack.Id.Value == "cnt-b");
        Assert.Empty(registry.Entries);
    }

    // ---------------------------------------------------------------------
    // A rejected pack does not block a healthy sibling.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_rejected_pack_does_not_stop_a_healthy_sibling_from_loading()
    {
        _packs.WriteFile("bad", "pack.json", "{ this is not json");
        _packs.WritePack("good", SystemPackJson("good-sys"));

        var registry = await Loader().LoadAsync();

        Assert.Single(registry.RejectedPacks);
        var goodPack = Assert.Single(registry.SystemPacks);
        Assert.Equal("good-sys", goodPack.Id.Value);
    }

    // ---------------------------------------------------------------------
    // An IO failure inside one candidate directory rejects only that pack, naming the file that
    // could not be read - it must never escape LoadAsync and take every other pack down with it.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_locked_template_file_rejects_only_its_own_pack_and_names_the_file()
    {
        _packs.WriteFile("locked", "pack.json", SystemPackJson("locked-sys"));
        _packs.WriteFile("locked", "templates/t.json", ValidTemplateJson);
        _packs.WritePack("good", SystemPackJson("good-sys"));

        var lockedTemplatePath = System.IO.Path.Combine(_packs.PackDirectory("locked"), "templates", "t.json");

        // FileShare.None makes the file genuinely unreadable by anyone else for as long as this
        // stream stays open, without touching real OS permissions - a deterministic stand-in for the
        // "IO exception mid-read" case the loader must survive.
        await using (new FileStream(lockedTemplatePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var registry = await Loader().LoadAsync();

            var rejected = Assert.Single(registry.RejectedPacks);
            Assert.Contains("t.json", rejected.Reason);

            // This is the assertion that matters most: one unreadable pack must never stop its
            // healthy sibling from loading normally.
            var goodPack = Assert.Single(registry.SystemPacks);
            Assert.Equal("good-sys", goodPack.Id.Value);
        }
    }

    // ---------------------------------------------------------------------
    // A missing packs directory is an empty registry, not an exception.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Reports_an_empty_registry_when_the_packs_directory_is_missing()
    {
        var loader = new ContentPackLoader(Path.Combine(_packs.Path, "does-not-exist"));

        var registry = await loader.LoadAsync();

        Assert.Empty(registry.SystemPacks);
        Assert.Empty(registry.ContentPacks);
        Assert.Empty(registry.Entries);
        Assert.Empty(registry.RejectedPacks);
    }

    // ---------------------------------------------------------------------
    // The three unresolved reasons.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task An_entry_naming_a_pack_that_is_not_installed_is_unresolved_as_missing_pack()
    {
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // points at "sys:thing", never installed

        var registry = await Loader().LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingPack, registered.Unresolved);
        Assert.Null(registered.Template);
    }

    [Fact]
    public async Task An_entry_naming_a_template_the_pack_does_not_declare_is_unresolved_as_missing_template()
    {
        const string otherTemplate = """
            {
              "id": "other",
              "name": "Other",
              "version": 1,
              "catalogVersion": 1,
              "fields": [],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", otherTemplate);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // points at "sys:thing", which is not declared

        var registry = await Loader().LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingTemplate, registered.Unresolved);
    }

    [Fact]
    public async Task An_entry_bound_to_a_template_at_a_different_version_is_unresolved_as_a_version_mismatch()
    {
        const string templateAtVersionTwo = """
            {
              "id": "thing",
              "name": "Thing",
              "version": 2,
              "catalogVersion": 1,
              "fields": [
                { "id": "label", "label": "Label", "type": "text" },
                { "id": "amount", "label": "Amount", "type": "integer" },
                { "id": "note", "label": "Note", "type": "text", "required": false }
              ],
              "card": []
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", templateAtVersionTwo);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // templateVersion 1, template is now at 2

        var registry = await Loader().LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.TemplateVersionMismatch, registered.Unresolved);
    }

    // ---------------------------------------------------------------------
    // An entry that contradicts a template it did resolve against rejects its whole content pack.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_the_content_pack_when_a_resolved_entry_is_missing_a_required_field()
    {
        const string entryMissingAmount = """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "sys:thing",
              "templateVersion": 1,
              "values": { "label": "A" }
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", ValidTemplateJson);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", entryMissingAmount);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("amount", rejected.Reason);
        Assert.Empty(registry.ContentPacks);
        Assert.Empty(registry.Entries);
    }

    [Fact]
    public async Task Rejects_the_content_pack_when_a_resolved_entry_has_the_wrong_value_type()
    {
        const string entryWithTextAmount = """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "sys:thing",
              "templateVersion": 1,
              "values": { "label": "A", "amount": "three" }
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", ValidTemplateJson);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", entryWithTextAmount);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("amount", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_the_content_pack_when_a_resolved_entry_supplies_an_undeclared_field()
    {
        const string entryWithExtraField = """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "sys:thing",
              "templateVersion": 1,
              "values": { "label": "A", "amount": 3, "mystery": "x" }
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", ValidTemplateJson);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", entryWithExtraField);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("mystery", rejected.Reason);
    }

    [Fact]
    public async Task An_optional_field_left_out_of_an_entry_is_legal()
    {
        const string entryWithoutOptionalNote = """
            {
              "id": "e1",
              "name": "Entry One",
              "template": "sys:thing",
              "templateVersion": 1,
              "values": { "label": "A", "amount": 3 }
            }
            """;
        _packs.WriteFile("sys", "pack.json", SystemPackJson("sys"));
        _packs.WriteFile("sys", "templates/t.json", ValidTemplateJson);
        _packs.WriteFile("cnt", "pack.json", ContentPackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", entryWithoutOptionalNote);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        var registered = Assert.Single(registry.Entries);
        Assert.Null(registered.Unresolved);
        Assert.NotNull(registered.Template);
    }
}
