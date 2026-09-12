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
        Assert.Empty(registry.RejectedEntries);
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
    // A duplicated entry id inside one pack: both colliding files are rejected, the pack still
    // loads, and neither file becomes a RegisteredEntry.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Rejects_a_duplicate_entry_id_within_a_pack()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/a.json", ValidEntryJson);
        _packs.WriteFile("cnt", "entries/b.json", ValidEntryJson);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        Assert.Equal(2, registry.RejectedEntries.Count);
        Assert.All(registry.RejectedEntries, rejected =>
        {
            Assert.Equal("cnt", rejected.Pack.Value);
            Assert.Contains("e1", rejected.Reason);
        });
        Assert.Equal(
            new[] { "entries/a.json", "entries/b.json" },
            registry.RejectedEntries.Select(rejected => rejected.Location).OrderBy(location => location, StringComparer.Ordinal));
    }

    // ---------------------------------------------------------------------
    // Entry file validation: unknown key, invalid content type reference, not valid JSON, invalid or
    // missing required fields, over the size limit. Every one of these marks only the one file - the
    // pack itself loads regardless.
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

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Equal("entries/e.json", rejected.Location);
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

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Contains(reference, rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_entry_file_that_is_not_valid_json()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", "{ this is not json");

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Contains("e.json", rejected.Reason);
    }

    [Theory]
    [InlineData("""{ "id": "Not Valid!", "name": "N", "template": "sys:thing", "templateVersion": 1, "values": {} }""", "id")]
    [InlineData("""{ "name": "N", "template": "sys:thing", "templateVersion": 1, "values": {} }""", "id")]
    [InlineData("""{ "id": "e1", "template": "sys:thing", "templateVersion": 1, "values": {} }""", "name")]
    [InlineData("""{ "id": "e1", "name": "N", "template": "sys:thing", "values": {} }""", "templateVersion")]
    public async Task Rejects_an_entry_file_with_an_invalid_or_missing_required_field(string json, string expectedInReason)
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Contains(expectedInReason, rejected.Reason);
    }

    [Fact]
    public async Task Rejects_an_entry_file_over_the_size_limit()
    {
        var oversizedName = new string('a', 1024 * 1024);
        var json = $$"""
            {
              "id": "e1",
              "name": "{{oversizedName}}",
              "template": "sys:thing",
              "templateVersion": 1,
              "values": { }
            }
            """;
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", json);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Single(registry.Packs);
        Assert.Empty(registry.Entries);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Contains("size limit", rejected.Reason);
    }

    // ---------------------------------------------------------------------
    // The heart of the change: a broken entry file beside a healthy one in the same pack marks only
    // the broken file. The healthy entry registers normally and the pack is not rejected.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_malformed_entry_file_beside_a_healthy_one_only_rejects_the_broken_file()
    {
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/good.json", ValidEntryJson);
        _packs.WriteFile("cnt", "entries/bad.json", "{ this is not json");

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        var pack = Assert.Single(registry.Packs);
        Assert.Equal("cnt", pack.Id.Value);

        var registered = Assert.Single(registry.Entries);
        Assert.Equal("e1", registered.Address.Entry.Value);

        var rejected = Assert.Single(registry.RejectedEntries);
        Assert.Equal("cnt", rejected.Pack.Value);
        Assert.Equal("entries/bad.json", rejected.Location);
    }

    // ---------------------------------------------------------------------
    // A broken manifest still rejects the whole pack, and never surfaces a RejectedEntry - the pack
    // never got far enough to read its entries directory at all.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task A_broken_manifest_still_rejects_the_whole_pack_with_no_rejected_entries()
    {
        _packs.WriteFile("bad", "pack.json", "{ this is not json");
        _packs.WriteFile("bad", "entries/e.json", ValidEntryJson);

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.Packs);
        Assert.Single(registry.RejectedPacks);
        Assert.Empty(registry.RejectedEntries);
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

    // A colliding pack's own entry problems must not leak into the registry either - the whole
    // directory is out, entries included, the moment the collision drops the pack.
    [Fact]
    public async Task A_pack_id_collision_hides_a_broken_entry_file_in_the_colliding_pack()
    {
        _packs.WritePack("first", PackJson("dup"));
        _packs.WriteFile("second", "pack.json", PackJson("dup"));
        _packs.WriteFile("second", "entries/bad.json", "{ this is not json");

        var registry = await Loader().LoadAsync();

        Assert.Empty(registry.Packs);
        Assert.Equal(2, registry.RejectedPacks.Count);
        Assert.Empty(registry.RejectedEntries);
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
    public async Task A_locked_entry_file_rejects_only_that_entry_and_names_the_file()
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

            // A locked entry file is now scoped to that one file, not the whole pack it lives in.
            Assert.Empty(registry.RejectedPacks);

            var rejected = Assert.Single(registry.RejectedEntries);
            Assert.Equal("locked-cnt", rejected.Pack.Value);
            Assert.Contains("e.json", rejected.Reason);

            // Both packs load - this is the assertion that matters most: one unreadable file must
            // never stop even its own pack, let alone a healthy sibling, from loading normally.
            Assert.Equal(2, registry.Packs.Count);
            Assert.Contains(registry.Packs, pack => pack.Id.Value == "locked-cnt");
            Assert.Contains(registry.Packs, pack => pack.Id.Value == "good-cnt");
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
        Assert.Empty(registry.RejectedEntries);
        Assert.Empty(registry.RejectedPacks);
    }

    // ---------------------------------------------------------------------
    // The one-pass resolution: MissingSet, MissingType, TypeVersionMismatch, ValuesRejected.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task An_entry_naming_a_set_that_is_not_installed_is_unresolved_as_missing_set()
    {
        // An entirely empty catalog answers HasSet("sys") with false, so this must fail at the
        // first check - before TryGet is ever asked - and never reach MissingType.
        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // points at "sys:thing", unknown to an empty catalog

        var registry = await Loader(FakeContentTypeCatalog.Empty()).LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingSet, registered.Unresolved);
        Assert.Null(registered.Type);
    }

    [Fact]
    public async Task An_entry_naming_an_installed_set_but_an_unknown_type_within_it_is_unresolved_as_missing_type()
    {
        // The catalog knows the set "sys" - it declares a sibling type "sys:other" - but it does
        // not declare "sys:thing", the type this entry names. HasSet("sys") must therefore answer
        // true while TryGet("sys:thing") still fails, landing on MissingType rather than MissingSet.
        var sibling = new ContentTypeReference(ContentId.Create("sys"), ContentId.Create("other"));
        var types = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(sibling, "Other", 1));

        _packs.WriteFile("cnt", "pack.json", PackJson("cnt"));
        _packs.WriteFile("cnt", "entries/e.json", ValidEntryJson); // points at "sys:thing"

        var registry = await Loader(types).LoadAsync();

        var registered = Assert.Single(registry.Entries);
        Assert.Equal(EntryUnresolvedReason.MissingType, registered.Unresolved);
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
