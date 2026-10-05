using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DungeonApp.Core.Entries;

namespace DungeonApp.Core.Tests.Entries.Content;

public sealed class ContentValuesTests
{
    /// <summary>
    /// A record standing in for whatever a system deserializes an envelope into. It describes
    /// nothing in particular on purpose: the engine does not know what kind of thing an entry is,
    /// and a test of the engine that needed a monster or a piece of gear to make its point would be
    /// asserting something the engine is not allowed to know.
    /// </summary>
    private sealed record Sample(string Title, int Size, string? Note = null);

    // The constructor is internal by design, so a test builds envelopes through the same public
    // door a system uses. A dictionary of raw elements is not a stand-in for any content
    // type's record - it is just the shape that lets a test choose property names and the exact
    // spelling of a value without naming anything real.
    private static ContentValues Envelope(string json) =>
        ContentValues.From(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!);

    private static Dictionary<string, JsonElement> Properties(ContentValues values) =>
        values.Read<Dictionary<string, JsonElement>>();

    private static string Raw(ContentValues values, string property) =>
        Properties(values)[property].GetRawText();

    private static string[] Snapshot(ContentValues values) =>
        Properties(values)
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => $"{property.Key}={property.Value.GetRawText()}")
            .ToArray();

    // ---------------------------------------------------------------------
    // Overlay - how an instance is read without the entry ever being copied.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_empty_envelope_carries_no_deviation()
    {
        Assert.True(ContentValues.Empty.IsEmpty);
        Assert.False(Envelope("""{ "title": "A" }""").IsEmpty);
    }

    [Fact]
    public void Overlay_lets_the_patch_win_over_a_property_the_entry_already_has()
    {
        var merged = Envelope("""{ "size": 7 }""").Overlay(Envelope("""{ "size": 9 }"""));

        Assert.Equal("9", Raw(merged, "size"));
    }

    [Fact]
    public void Overlay_admits_a_property_the_entry_never_had()
    {
        // An instance may deviate in a property the entry left out entirely, so a patch key with no
        // counterpart in the entry has to survive the merge rather than be dropped as unknown.
        var merged = Envelope("""{ "title": "A" }""").Overlay(Envelope("""{ "note": "scarred" }"""));

        Assert.Equal("\"scarred\"", Raw(merged, "note"));
        Assert.Equal("\"A\"", Raw(merged, "title"));
    }

    [Fact]
    public void Overlay_passes_an_untouched_entry_property_through_unchanged()
    {
        var merged = Envelope("""{ "title": "A", "size": 7 }""").Overlay(Envelope("""{ "size": 9 }"""));

        Assert.Equal("\"A\"", Raw(merged, "title"));
    }

    [Fact]
    public void Overlay_with_an_empty_patch_is_the_entry_itself()
    {
        // The common case by far: most instances deviate in nothing, and reading one must give back
        // exactly what the pack ships.
        var entry = Envelope("""{ "title": "A", "size": 7 }""");

        Assert.Equal(Snapshot(entry), Snapshot(entry.Overlay(ContentValues.Empty)));
    }

    [Fact]
    public void Overlay_merges_a_nested_object_key_by_key()
    {
        var merged = Envelope("""{ "note": { "a": 1, "b": 2 } }""").Overlay(Envelope("""{ "note": { "b": 3 } }"""));

        Assert.Equal("""{"a":1,"b":3}""", Raw(merged, "note"));
    }

    [Fact]
    public void Overlay_merges_objects_at_every_depth_and_adds_nested_keys_the_entry_lacks()
    {
        var merged = Envelope("""{ "outer": { "inner": { "a": 1, "b": 2 }, "c": 3 } }""")
            .Overlay(Envelope("""{ "outer": { "inner": { "b": 5, "d": 6 } } }"""));

        Assert.Equal("""{"inner":{"a":1,"b":5,"d":6},"c":3}""", Raw(merged, "outer"));
    }

    [Fact]
    public void Overlay_replaces_an_array_whole()
    {
        // An array has no keys to merge by, so the patch's array is the instance's array.
        var merged = Envelope("""{ "tags": [1, 2, 3] }""").Overlay(Envelope("""{ "tags": [9] }"""));

        Assert.Equal("[9]", Raw(merged, "tags"));
    }

    [Fact]
    public void Overlay_replaces_a_value_whose_kind_differs_on_the_two_sides()
    {
        var objectOverText = Envelope("""{ "note": "x" }""").Overlay(Envelope("""{ "note": { "a": 1 } }"""));
        var textOverObject = Envelope("""{ "note": { "a": 1 } }""").Overlay(Envelope("""{ "note": "x" }"""));

        Assert.Equal("""{"a":1}""", Raw(objectOverText, "note"));
        Assert.Equal("\"x\"", Raw(textOverObject, "note"));
    }

    [Fact]
    public void Overlay_treats_null_as_a_value_not_as_a_removal()
    {
        var merged = Envelope("""{ "size": 7, "note": { "a": 1 } }""")
            .Overlay(Envelope("""{ "size": null, "note": { "a": null } }"""));

        Assert.Equal("null", Raw(merged, "size"));
        Assert.Equal("""{"a":null}""", Raw(merged, "note"));
    }
    [Fact]
    public void Overlay_modifies_neither_side()
    {
        // The entry is shared by every instance pointing at it, so a merge that wrote into either
        // operand would leak one instance's deviation into all the others.
        var entry = Envelope("""{ "title": "A", "size": 7 }""");
        var patch = Envelope("""{ "size": 9, "note": "scarred" }""");
        var before = (Entry: Snapshot(entry), Patch: Snapshot(patch));

        entry.Overlay(patch);

        Assert.Equal(before.Entry, Snapshot(entry));
        Assert.Equal(before.Patch, Snapshot(patch));
    }

    // ---------------------------------------------------------------------
    // Difference - how an edited instance is saved without materializing the entry.
    // ---------------------------------------------------------------------

    [Fact]
    public void Difference_records_a_property_whose_value_moved()
    {
        var patch = ContentValues.Difference(Envelope("""{ "size": 7 }"""), Envelope("""{ "size": 9 }"""));

        Assert.Equal("9", Raw(patch, "size"));
    }

    [Fact]
    public void Difference_leaves_out_a_property_that_still_matches_the_entry()
    {
        // The patch has to stay sparse: a key copied here that merely repeats the entry would pin
        // the instance to today's value and defeat the reason instances link instead of copy.
        var patch = ContentValues.Difference(
            Envelope("""{ "title": "A", "size": 7 }"""),
            Envelope("""{ "title": "A", "size": 9 }"""));

        Assert.False(Properties(patch).ContainsKey("title"));
        Assert.Single(Properties(patch));
    }

    [Fact]
    public void Difference_records_a_property_the_entry_does_not_have()
    {
        var patch = ContentValues.Difference(Envelope("""{ "title": "A" }"""), Envelope("""{ "title": "A", "note": "scarred" }"""));

        Assert.Equal("\"scarred\"", Raw(patch, "note"));
    }

    [Fact]
    public void Difference_reports_only_the_nested_keys_that_moved()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "combat": { "hp": 7, "ac": 15, "currentHp": 7 }, "title": "A" }"""),
            Envelope("""{ "combat": { "hp": 7, "ac": 15, "currentHp": 3 }, "title": "A" }"""));

        Assert.Equal("""{"currentHp":3}""", Raw(patch, "combat"));
        Assert.Single(Properties(patch));
    }

    [Fact]
    public void Difference_leaves_out_a_nested_object_that_still_matches_the_entry()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "combat": { "hp": 7 }, "size": 1 }"""),
            Envelope("""{ "combat": { "hp": 7 }, "size": 2 }"""));

        Assert.False(Properties(patch).ContainsKey("combat"));
    }

    [Fact]
    public void Difference_reports_a_nested_key_the_entry_does_not_have()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "combat": { "hp": 7 } }"""),
            Envelope("""{ "combat": { "hp": 7, "currentHp": 3 } }"""));

        Assert.Equal("""{"currentHp":3}""", Raw(patch, "combat"));
    }

    [Fact]
    public void Difference_reports_a_changed_array_whole()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "tags": [1, 2, 3] }"""),
            Envelope("""{ "tags": [1, 2, 4] }"""));

        Assert.Equal("[1,2,4]", Raw(patch, "tags"));
    }

    [Fact]
    public void Difference_ignores_a_nested_key_the_candidate_dropped()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "combat": { "hp": 7, "ac": 15 } }"""),
            Envelope("""{ "combat": { "hp": 7 } }"""));

        Assert.True(patch.IsEmpty);
    }

    [Fact]
    public void Overlaying_the_difference_gives_back_the_candidate_for_nested_data()
    {
        var baseline = Envelope("""{ "title": "A", "combat": { "hp": 7, "ac": 15, "extra": { "x": 1, "y": 2 } }, "tags": [1, 2] }""");
        var candidate = Envelope("""{ "title": "A", "combat": { "hp": 7, "ac": 12, "currentHp": 3, "extra": { "x": 1, "y": 5 } }, "tags": [2] }""");

        var restored = baseline.Overlay(ContentValues.Difference(baseline, candidate));

        // Key order inside an object is not part of its meaning, so the comparison is by value.
        Assert.True(JsonElement.DeepEquals(
            JsonSerializer.SerializeToElement(Properties(candidate)),
            JsonSerializer.SerializeToElement(Properties(restored))));
    }
    [Fact]
    public void Difference_ignores_a_property_the_candidate_dropped()
    {
        // A patch is an overlay and cannot say "remove this key". Giving it one would let an
        // instance reshape its entry rather than deviate from it.
        var patch = ContentValues.Difference(Envelope("""{ "title": "A", "size": 7 }"""), Envelope("""{ "title": "A" }"""));

        Assert.True(patch.IsEmpty);
    }

    [Fact]
    public void Difference_between_identical_envelopes_is_empty()
    {
        var patch = ContentValues.Difference(
            Envelope("""{ "title": "A", "size": 7 }"""),
            Envelope("""{ "title": "A", "size": 7 }"""));

        Assert.True(patch.IsEmpty);
    }

    [Fact]
    public void Difference_does_not_invent_a_deviation_from_how_a_number_was_written()
    {
        // Comparing raw text would call these two different and save a deviation nobody made, which
        // is the whole reason the comparison goes through DeepEquals instead.
        var patch = ContentValues.Difference(Envelope("""{ "size": 7 }"""), Envelope("""{ "size": 7.0 }"""));

        Assert.True(patch.IsEmpty);
    }

    // ---------------------------------------------------------------------
    // The return leg: a record edited by a system and turned back into a patch.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_edit_to_one_property_comes_back_as_a_patch_of_one_property()
    {
        // The round trip is the real contract - read, edit, write back - and it only holds if the
        // keys written out land on the keys the entry already uses.
        var entry = Envelope("""{ "title": "A", "size": 7 }""");
        var edited = entry.Read<Sample>() with { Size = 9 };

        var patch = ContentValues.Difference(entry, ContentValues.From(edited));

        Assert.Equal("9", Raw(patch, "size"));
        Assert.Single(Properties(patch));
    }

    [Fact]
    public void An_optional_property_left_null_is_not_written_out()
    {
        // Content records are mostly optional properties. Were unset ones written as explicit
        // nulls, every one of them would read as a deviation and the patch would overlay nulls over
        // the entry's own values.
        var written = Properties(ContentValues.From(new Sample("A", 7)));

        Assert.False(written.ContainsKey("note"));
        Assert.Equal(new[] { "size", "title" }, written.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Clearing_a_deviation_back_to_null_drops_its_key_from_the_patch()
    {
        // The other half of the same rule: putting a property back to null is how a GM undoes a
        // deviation, and the key has to disappear so reading falls back to the entry.
        var entry = Envelope("""{ "title": "A", "size": 7 }""");
        var restored = (entry.Read<Sample>() with { Note = "scarred" }) with { Note = null };

        Assert.True(ContentValues.Difference(entry, ContentValues.From(restored)).IsEmpty);
    }

    // ---------------------------------------------------------------------
    // An envelope over something that is not an object should never exist. If one does, it is heard.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_envelope_that_is_not_an_object_is_refused_by_every_operation()
    {
        // Quietly doing nothing with one would lose an edit without saying so.
        var array = ContentValues.From(new[] { 1, 2, 3 });
        var number = ContentValues.From(7);
        var objectValued = Envelope("""{ "size": 7 }""");

        Assert.Throws<InvalidOperationException>(() => array.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => number.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => array.Overlay(objectValued));
        Assert.Throws<InvalidOperationException>(() => objectValued.Overlay(array));
        Assert.Throws<InvalidOperationException>(() => ContentValues.Difference(array, objectValued));
        Assert.Throws<InvalidOperationException>(() => ContentValues.Difference(objectValued, array));
    }
}
