using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Core.Tests.Fakes;

namespace DungeonApp.Core.Tests.Content;

public sealed class ContentPackLoaderTests : IDisposable
{
    private readonly TemporaryPacks _packs = new();

    private const string ValidEntryJson = """
        {
          "id": "e1",
          "name": "Entry One",
          "template": "sys:thing",
          "templateVersion": 1,
          "values": { "label": "A" }
        }
        """;

    public void Dispose() => _packs.Dispose();

    private ContentPackLoader Loader(IContentTypeCatalog? types = null) => new(_packs.Path, types ?? FakeContentTypeCatalog.Empty());

    private static string PackJson(string id, string name = "Pack", int major = 1, int minor = 0, int formatVersion = 1) =>
        $$"""
        {
          "formatVersion": {{formatVersion}},
          "id": "{{id}}",
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
        var monster = new ContentTypeReference(ContentId.Create("dnd5e"), ContentId.Create("monster"));
        var gear = new ContentTypeReference(ContentId.Create("dnd5e"), ContentId.Create("gear"));
        var types = FakeContentTypeCatalog.Of(
            new ContentTypeDescriptor(monster, "Monster", 1),
            new ContentTypeDescriptor(gear, "Gear", 1));

        var registry = await new ContentPackLoader(RepositoryRoot.PackFixtures, types).LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Equal(2, registry.Packs.Count);
        Assert.Equal(3, registry.Entries.Count);
        Assert.All(registry.Entries, entry => Assert.Null(entry.Unresolved));

        var goblin = registry.Entries.Single(entry => entry.Address.Entry.Value == "goblin");

        Assert.Equal("goblinoids", goblin.Address.Pack.Value);
        Assert.Equal(monster, goblin.Entry.Type);
        Assert.Equal("Monster", goblin.Type!.Value.Name);

        // A plain Dictionary<string, JsonElement> is not a stand-in for any content type's own
        // record - it just proves ContentValues.Read<T> deserializes whatever T a caller asks for,
        // without this test ever naming a content type's shape.
        var values = goblin.Entry.Values.Read<Dictionary<string, JsonElement>>();
        Assert.Equal(15, values["ac"].GetInt32());
        Assert.Equal("1/4 (50 PD)", values["challenge"].GetString());
    }

    // ---------------------------------------------------------------------
    // Manifest: missing pack.json, invalid JSON, unknown formatVersion, unknown key (e.g. a
    // leftover "kind" - the old two-kind-of-pack split's field, now just an unmapped member).
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
        _packs.WritePack("bad", PackJson("sys", formatVersion: 99));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("formatVersion", rejected.Reason);
        Assert.Contains("99", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_json_with_an_unknown_key()
    {
        const string json = """
            {
              "formatVersion": 1,
              "id": "sys",
              "kind": "system",
              "name": "Sys",
              "version": { "major": 1, "minor": 0 }
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("pack.json", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_invalid_pack_id()
    {
        _packs.WritePack("bad", PackJson("Not Valid!"));

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
              "version": { "major": 1, "minor": 0 }
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("name", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_with_no_version()
    {
        const string json = """
            {
              "formatVersion": 1,
              "id": "sys",
              "name": "Sys"
            }
            """;
        _packs.WritePack("bad", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("version", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // A "templates" directory, if present, is inert - never inspected, never a rejection.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_leftover_templates_directory_is_silently_ignored()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "templates/whatever.json", "{ this is not even valid json");
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Entries);
    }

    // ---------------------------------------------------------------------
    // A duplicated entry id inside one pack.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_duplicate_entry_id_within_a_pack()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/a.json", ValidEntryJson);
        _packs.WriteFile("cnt", "entries/b.json", ValidEntryJson);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("e1", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Entry file validation: unknown key, invalid content type reference.
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
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("e.json", rejected.Reason);
    }

    [Theory]
    [InlineData("systhing")]
    [InlineData("sys:sub:thing")]
    [InlineData("Sys:thing")]
    public async Task Rejects_an_invalid_content_type_reference(string reference)
    {
        var json = $$"""
            {
              "id": "e1",
              "name": "Entry One",
              "template": "{{reference}}",
              "templateVersion": 1,
              "values": { }
            }
            """;
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains(reference, rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // Two installed packs with the same id are both rejected.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_both_packs_when_two_share_an_id()
    {
        _packs.WritePack("first", PackJson("dup"));
        _packs.WritePack("second", PackJson("dup"));

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.Packs);
        Assert.Equal(2, registry.RejectedPacks.Count);
        Assert.All(registry.RejectedPacks, rejected => Assert.Contains("dup", rejected.Reason));
    }

    // ---------------------------------------------------------------------
    // Size and count limits.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_pack_json_over_the_size_limit()
    {
        var oversizedName = new string('a', 1024 * 1024);
        _packs.WritePack("bad", PackJson("sys", name: oversizedName));

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("pack.json", rejected.Reason);
        Assert.Contains("size limit", rejected.Reason);
    }

    [Fact]
    public async Task Rejects_a_pack_with_more_than_ten_thousand_entries()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));

        for (var i = 0; i < 10_001; i++)
        {
            _packs.WriteFile("cnt", $"entries/e{i}.json", "{}");
        }

        var registry = await Loader().LoadAsync();

        var rejected = Assert.Single(registry.RejectedPacks);
        Assert.Contains("10000", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // A rejected pack does not block a healthy sibling.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_rejected_pack_does_not_stop_a_healthy_sibling_from_loading()
    {
        _packs.WriteFile("bad", "pack.json", "{ this is not json");
        _packs.WritePack("good", PackJson("good-cnt"));

        var registry = await Loader().LoadAsync();

        Assert.Single(registry.RejectedPacks);
        var goodPack = Assert.Single(registry.Packs);
        Assert.Equal("good-cnt", goodPack.Id.Value);
    }

    // ---------------------------------------------------------------------
    // An IO failure inside one candidate directory rejects only that pack, naming the file that
    // could not be read - it must never escape LoadAsync and take every other pack down with it.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_locked_entry_file_rejects_only_its_own_pack_and_names_the_file()
    {
        _packs.WriteFile("locked", "pack.json", PackJson("locked-cnt"));
        _packs.WriteFile("locked", "entries/e.json", ValidEntryJson);
        _packs.WritePack("good", PackJson("good-cnt"));

        var lockedEntryPath = Path.Combine(_packs.PackDirectory("locked"), "entries", "e.json");

        // FileShare.None makes the file genuinely unreadable by anyone else for as long as this
        // stream stays open, without touching real OS permissions - a deterministic stand-in for the
        // "IO exception mid-read" case the loader must survive.
        await using (new FileStream(lockedEntryPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var registry = await Loader().LoadAsync();

            var rejected = Assert.Single(registry.RejectedPacks);
            Assert.Contains("e.json", rejected.Reason);

            // This is the assertion that matters most: one unreadable pack must never stop its
            // healthy sibling from loading normally.
            var goodPack = Assert.Single(registry.Packs);
            Assert.Equal("good-cnt", goodPack.Id.Value);
        }
    }

    // ---------------------------------------------------------------------
    // A missing packs directory is an empty registry, not an exception.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Reports_an_empty_registry_when_the_packs_directory_is_missing()
    {
        var loader = new ContentPackLoader(Path.Combine(_packs.Path, "does-not-exist"), FakeContentTypeCatalog.Empty());

        var registry = await loader.LoadAsync();

        Assert.Empty(registry.Packs);
        Assert.Empty(registry.Entries);
        Assert.Empty(registry.RejectedPacks);
    }

    // ---------------------------------------------------------------------
    // The one-pass resolution: MissingSet, TypeVersionMismatch, ValuesRejected.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task An_entry_naming_an_unknown_content_type_reference_is_unresolved_as_missing_set()
    {
        // IContentTypeCatalog.TryGet's two-method contract cannot tell "no content set uses this
        // id at all" apart from "a content set is known, but it does not declare this type" - see
        // EntryUnresolvedReason.MissingSet's remarks. Both collapse to MissingSet, which is exactly
        // what this test locks in: an entirely empty catalog produces the same outcome a catalog
        // that merely lacks this one type would.
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // points at "sys:thing", unknown to an empty catalog

        var registry = await Loader(FakeContentTypeCatalog.Empty()).LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingSet, registered.Unresolved);
        Assert.Null(registered.Type);
    }

    [Fact]
    public async Task An_entry_bound_to_a_content_type_at_a_different_version_is_unresolved_as_a_version_mismatch()
    {
        var reference = new ContentTypeReference(ContentId.Create("sys"), ContentId.Create("thing"));
        var types = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(reference, "Thing", 2));

        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // declares templateVersion 1, catalog is at 2

        var registry = await Loader(types).LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.TypeVersionMismatch, registered.Unresolved);
    }

    [Fact]
    public async Task An_entry_whose_values_the_catalog_rejects_is_unresolved_with_the_catalogs_own_reason()
    {
        var reference = new ContentTypeReference(ContentId.Create("sys"), ContentId.Create("thing"));
        var types = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(reference, "Thing", 1)],
            new HashSet<ContentTypeReference> { reference });

        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson);

        var registry = await Loader(types).LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, registered.Unresolved);
        Assert.Equal("rejected by test fake.", registered.UnresolvedDetail);
    }

    [Fact]
    public async Task A_values_rejection_marks_only_that_entry_and_never_its_pack()
    {
        var accepted = new ContentTypeReference(ContentId.Create("sys"), ContentId.Create("good"));
        var rejected = new ContentTypeReference(ContentId.Create("sys"), ContentId.Create("bad"));
        var types = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(accepted, "Good", 1), new ContentTypeDescriptor(rejected, "Bad", 1)],
            new HashSet<ContentTypeReference> { rejected });

        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/ok.json", """
            {
              "id": "ok",
              "name": "Ok",
              "template": "sys:good",
              "templateVersion": 1,
              "values": { }
            }
            """);
        _packs.WriteFile("cnt", "entries/bad.json", """
            {
              "id": "bad-entry",
              "name": "Bad",
              "template": "sys:bad",
              "templateVersion": 1,
              "values": { }
            }
            """);

        var registry = await Loader(types).LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);

        var ok = registry.Entries.Single(entry => entry.Address.Entry.Value == "ok");
        var badEntry = registry.Entries.Single(entry => entry.Address.Entry.Value == "bad-entry");

        Assert.Null(ok.Unresolved);
        Assert.Equal(EntryUnresolvedReason.ValuesRejected, badEntry.Unresolved);
    }
}
