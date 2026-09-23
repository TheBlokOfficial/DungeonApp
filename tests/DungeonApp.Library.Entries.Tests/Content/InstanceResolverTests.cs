using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Entries.Instances;
using DungeonApp.Library.Entries.Tests.Fakes;

namespace DungeonApp.Library.Entries.Tests.Content;

public sealed class InstanceResolverTests
{
    private static readonly ContentId PackId = ContentId.Create("bestiary");
    private static readonly ContentId EntryId = ContentId.Create("goblin");
    private static readonly EntryAddress Address = new(PackId, EntryId);
    private static readonly ContentTypeReference Type = new(ContentId.Create("sample-set"), ContentId.Create("sample-type"));

    // A dictionary of raw elements stands in for whatever a system would deserialize an
    // envelope into - the engine (and this resolver) never names a real content type's shape, so
    // neither does a test of it.
    private static ContentValues Envelope(string json) =>
        ContentValues.From(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!);

    private static string[] Snapshot(ContentValues values) =>
        values.Read<Dictionary<string, JsonElement>>()
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => $"{property.Key}={property.Value.GetRawText()}")
            .ToArray();

    private static Entry MakeEntry(ContentValues values) =>
        new(EntryId, "Goblin", Type, TypeVersion: 1, values);

    private static Pack MakePack(params Entry[] entries) =>
        new(PackId, "Bestiary", new PackVersion(1, 0), entries);

    private static ContentRegistry MakeRegistry(Pack pack, params RegisteredEntry[] entries) =>
        new([pack], entries, [], []);

    private static CampaignInstance MakeInstance(ContentValues patch, EntryAddress? source = null) =>
        new()
        {
            Id = InstanceId.New(),
            Source = source ?? Address,
            Label = "Goblin 2",
            Patch = patch,
        };

    // ---------------------------------------------------------------------
    // The resolved path.
    // ---------------------------------------------------------------------

    [Fact]
    public void A_resolved_instance_carries_the_patch_over_the_entry_and_leaves_untouched_properties_alone()
    {
        var entryValues = Envelope("""{ "title": "A", "size": 7 }""");
        var entry = MakeEntry(entryValues);
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var instance = MakeInstance(Envelope("""{ "size": 9 }"""));
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Null(resolved.Unresolved);
        Assert.Same(registered, resolved.Source);
        Assert.Equal("9", resolved.Values!.Read<Dictionary<string, JsonElement>>()["size"].GetRawText());
        Assert.Equal("\"A\"", resolved.Values!.Read<Dictionary<string, JsonElement>>()["title"].GetRawText());
    }

    [Fact]
    public void An_empty_patch_resolves_to_exactly_the_entrys_own_values()
    {
        var entryValues = Envelope("""{ "title": "A", "size": 7 }""");
        var entry = MakeEntry(entryValues);
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var instance = MakeInstance(ContentValues.Empty);
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Equal(Snapshot(entryValues), Snapshot(resolved.Values!));
    }

    // ---------------------------------------------------------------------
    // The four ways an instance fails to reach its content.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_instance_pointing_at_an_uninstalled_pack_is_unresolved_as_missing_pack()
    {
        var registry = new ContentRegistry([], [], [], []);
        var catalog = FakeContentTypeCatalog.Empty();

        var instance = MakeInstance(ContentValues.Empty);
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Equal(InstanceUnresolvedReason.MissingPack, resolved.Unresolved);
        Assert.Null(resolved.Source);
        Assert.Null(resolved.Values);
    }

    [Fact]
    public void An_instance_pointing_at_an_entry_the_installed_pack_does_not_have_is_unresolved_as_missing_entry()
    {
        // The pack is installed - just not under the entry id this instance names.
        var otherEntry = MakeEntry(Envelope("{}")) with { Id = ContentId.Create("orc") };
        var registered = RegisteredEntry.CreateResolved(
            new EntryAddress(PackId, otherEntry.Id), otherEntry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(otherEntry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var instance = MakeInstance(ContentValues.Empty);
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Equal(InstanceUnresolvedReason.MissingEntry, resolved.Unresolved);
        Assert.Null(resolved.Source);
    }

    [Fact]
    public void An_instance_whose_entry_never_bound_to_a_content_type_is_unresolved_as_entry_unresolved()
    {
        var entry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var registered = RegisteredEntry.CreateUnresolved(Address, entry, EntryUnresolvedReason.MissingSet);
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Empty();

        var instance = MakeInstance(ContentValues.Empty);
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Equal(InstanceUnresolvedReason.EntryUnresolved, resolved.Unresolved);
        Assert.Same(registered, resolved.Source);
        Assert.Null(resolved.Values);
    }

    [Fact]
    public void An_instance_whose_merged_values_the_catalog_rejects_is_unresolved_as_values_rejected_with_the_catalogs_own_message()
    {
        var entry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(Type, "Sample", 1)],
            new HashSet<ContentTypeReference> { Type });

        var instance = MakeInstance(Envelope("""{ "note": "scarred" }"""));
        var resolved = new InstanceResolver(registry, catalog).Resolve(instance);

        Assert.Equal(InstanceUnresolvedReason.ValuesRejected, resolved.Unresolved);
        Assert.Equal("rejected by test fake.", resolved.UnresolvedDetail);
        Assert.Same(registered, resolved.Source);
        Assert.Null(resolved.Values);
    }

    // ---------------------------------------------------------------------
    // The patch must come back untouched in every unresolved case - never trimmed, cleared, or
    // otherwise "repaired" on the instance's way back out.
    // ---------------------------------------------------------------------

    [Fact]
    public void The_instances_patch_survives_every_unresolved_outcome_unchanged()
    {
        var patch = Envelope("""{ "note": "scarred", "size": 9 }""");
        var patchSnapshot = Snapshot(patch);

        var missingPackInstance = MakeInstance(patch);
        var missingPackResult = new InstanceResolver(new ContentRegistry([], [], [], []), FakeContentTypeCatalog.Empty())
            .Resolve(missingPackInstance);
        Assert.Same(missingPackInstance, missingPackResult.Instance);
        Assert.Equal(patchSnapshot, Snapshot(missingPackResult.Instance.Patch));

        var otherEntry = MakeEntry(Envelope("{}")) with { Id = ContentId.Create("orc") };
        var missingEntryRegistry = MakeRegistry(
            MakePack(otherEntry),
            RegisteredEntry.CreateResolved(
                new EntryAddress(PackId, otherEntry.Id), otherEntry, new ContentTypeDescriptor(Type, "Sample", 1)));
        var missingEntryInstance = MakeInstance(patch);
        var missingEntryResult = new InstanceResolver(missingEntryRegistry, FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1)))
            .Resolve(missingEntryInstance);
        Assert.Equal(patchSnapshot, Snapshot(missingEntryResult.Instance.Patch));

        var unresolvedEntry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var entryUnresolvedRegistry = MakeRegistry(
            MakePack(unresolvedEntry),
            RegisteredEntry.CreateUnresolved(Address, unresolvedEntry, EntryUnresolvedReason.MissingSet));
        var entryUnresolvedInstance = MakeInstance(patch);
        var entryUnresolvedResult = new InstanceResolver(entryUnresolvedRegistry, FakeContentTypeCatalog.Empty())
            .Resolve(entryUnresolvedInstance);
        Assert.Equal(patchSnapshot, Snapshot(entryUnresolvedResult.Instance.Patch));

        var rejectingEntry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var rejectingRegistry = MakeRegistry(
            MakePack(rejectingEntry),
            RegisteredEntry.CreateResolved(Address, rejectingEntry, new ContentTypeDescriptor(Type, "Sample", 1)));
        var rejectingCatalog = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(Type, "Sample", 1)],
            new HashSet<ContentTypeReference> { Type });
        var valuesRejectedInstance = MakeInstance(patch);
        var valuesRejectedResult = new InstanceResolver(rejectingRegistry, rejectingCatalog).Resolve(valuesRejectedInstance);
        Assert.Equal(patchSnapshot, Snapshot(valuesRejectedResult.Instance.Patch));
    }

    // ---------------------------------------------------------------------
    // The catalog must see the merged envelope, not the entry's own - proving the whole point of
    // validating after the overlay rather than before it.
    // ---------------------------------------------------------------------

    [Fact]
    public void The_catalog_is_asked_to_validate_the_merged_values_not_the_entrys_own()
    {
        var entry = MakeEntry(Envelope("""{ "title": "A", "size": 7 }"""));
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var instance = MakeInstance(Envelope("""{ "size": 9 }"""));
        new InstanceResolver(registry, catalog).Resolve(instance);

        var call = Assert.Single(catalog.ValidateCalls);
        Assert.Equal(Type, call.Reference);

        var seen = call.Values.Read<Dictionary<string, JsonElement>>();
        Assert.Equal("9", seen["size"].GetRawText());
        Assert.NotEqual("7", seen["size"].GetRawText());
    }
}
