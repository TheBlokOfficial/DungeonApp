using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.Tests.Entries.Fakes;

namespace DungeonApp.Core.Tests.Entries.Content;

public sealed class EntityResolverTests
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

    private static CampaignEntity MakeEntity(ContentValues patch, EntryAddress? source = null) =>
        new()
        {
            Id = EntityId.New(),
            Source = source ?? Address,
            Label = "Goblin 2",
            Patch = patch,
        };

    // ---------------------------------------------------------------------
    // The resolved path.
    // ---------------------------------------------------------------------

    [Fact]
    public void A_resolved_entity_carries_the_patch_over_the_entry_and_leaves_untouched_properties_alone()
    {
        var entryValues = Envelope("""{ "title": "A", "size": 7 }""");
        var entry = MakeEntry(entryValues);
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var entity = MakeEntity(Envelope("""{ "size": 9 }"""));
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

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

        var entity = MakeEntity(ContentValues.Empty);
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

        Assert.Equal(Snapshot(entryValues), Snapshot(resolved.Values!));
    }

    // ---------------------------------------------------------------------
    // The four ways an entity fails to reach its content.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_entity_pointing_at_an_uninstalled_pack_is_unresolved_as_missing_pack()
    {
        var registry = new ContentRegistry([], [], [], []);
        var catalog = FakeContentTypeCatalog.Empty();

        var entity = MakeEntity(ContentValues.Empty);
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

        Assert.Equal(EntityUnresolvedReason.MissingPack, resolved.Unresolved);
        Assert.Null(resolved.Source);
        Assert.Null(resolved.Values);
    }

    [Fact]
    public void An_entity_pointing_at_an_entry_the_installed_pack_does_not_have_is_unresolved_as_missing_entry()
    {
        // The pack is installed - just not under the entry id this entity names.
        var otherEntry = MakeEntry(Envelope("{}")) with { Id = ContentId.Create("orc") };
        var registered = RegisteredEntry.CreateResolved(
            new EntryAddress(PackId, otherEntry.Id), otherEntry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(otherEntry), registered);
        var catalog = FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1));

        var entity = MakeEntity(ContentValues.Empty);
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

        Assert.Equal(EntityUnresolvedReason.MissingEntry, resolved.Unresolved);
        Assert.Null(resolved.Source);
    }

    [Fact]
    public void An_entity_whose_entry_never_bound_to_a_content_type_is_unresolved_as_entry_unresolved()
    {
        var entry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var registered = RegisteredEntry.CreateUnresolved(Address, entry, EntryUnresolvedReason.MissingSet);
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = FakeContentTypeCatalog.Empty();

        var entity = MakeEntity(ContentValues.Empty);
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

        Assert.Equal(EntityUnresolvedReason.EntryUnresolved, resolved.Unresolved);
        Assert.Same(registered, resolved.Source);
        Assert.Null(resolved.Values);
    }

    [Fact]
    public void An_entity_whose_merged_values_the_catalog_rejects_is_unresolved_as_values_rejected_with_the_catalogs_own_message()
    {
        var entry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var registered = RegisteredEntry.CreateResolved(Address, entry, new ContentTypeDescriptor(Type, "Sample", 1));
        var registry = MakeRegistry(MakePack(entry), registered);
        var catalog = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(Type, "Sample", 1)],
            new HashSet<ContentTypeReference> { Type });

        var entity = MakeEntity(Envelope("""{ "note": "scarred" }"""));
        var resolved = new EntityResolver(registry, catalog).Resolve(entity);

        Assert.Equal(EntityUnresolvedReason.ValuesRejected, resolved.Unresolved);
        Assert.Equal("rejected by test fake.", resolved.UnresolvedDetail);
        Assert.Same(registered, resolved.Source);
        Assert.Null(resolved.Values);
    }

    // ---------------------------------------------------------------------
    // The patch must come back untouched in every unresolved case - never trimmed, cleared, or
    // otherwise "repaired" on the entity's way back out.
    // ---------------------------------------------------------------------

    [Fact]
    public void The_entities_patch_survives_every_unresolved_outcome_unchanged()
    {
        var patch = Envelope("""{ "note": "scarred", "size": 9 }""");
        var patchSnapshot = Snapshot(patch);

        var missingPackEntity = MakeEntity(patch);
        var missingPackResult = new EntityResolver(new ContentRegistry([], [], [], []), FakeContentTypeCatalog.Empty())
            .Resolve(missingPackEntity);
        Assert.Same(missingPackEntity, missingPackResult.Entity);
        Assert.Equal(patchSnapshot, Snapshot(missingPackResult.Entity.Patch));

        var otherEntry = MakeEntry(Envelope("{}")) with { Id = ContentId.Create("orc") };
        var missingEntryRegistry = MakeRegistry(
            MakePack(otherEntry),
            RegisteredEntry.CreateResolved(
                new EntryAddress(PackId, otherEntry.Id), otherEntry, new ContentTypeDescriptor(Type, "Sample", 1)));
        var missingEntryEntity = MakeEntity(patch);
        var missingEntryResult = new EntityResolver(missingEntryRegistry, FakeContentTypeCatalog.Of(new ContentTypeDescriptor(Type, "Sample", 1)))
            .Resolve(missingEntryEntity);
        Assert.Equal(patchSnapshot, Snapshot(missingEntryResult.Entity.Patch));

        var unresolvedEntry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var entryUnresolvedRegistry = MakeRegistry(
            MakePack(unresolvedEntry),
            RegisteredEntry.CreateUnresolved(Address, unresolvedEntry, EntryUnresolvedReason.MissingSet));
        var entryUnresolvedEntity = MakeEntity(patch);
        var entryUnresolvedResult = new EntityResolver(entryUnresolvedRegistry, FakeContentTypeCatalog.Empty())
            .Resolve(entryUnresolvedEntity);
        Assert.Equal(patchSnapshot, Snapshot(entryUnresolvedResult.Entity.Patch));

        var rejectingEntry = MakeEntry(Envelope("""{ "title": "A" }"""));
        var rejectingRegistry = MakeRegistry(
            MakePack(rejectingEntry),
            RegisteredEntry.CreateResolved(Address, rejectingEntry, new ContentTypeDescriptor(Type, "Sample", 1)));
        var rejectingCatalog = new FakeContentTypeCatalog(
            [new ContentTypeDescriptor(Type, "Sample", 1)],
            new HashSet<ContentTypeReference> { Type });
        var valuesRejectedEntity = MakeEntity(patch);
        var valuesRejectedResult = new EntityResolver(rejectingRegistry, rejectingCatalog).Resolve(valuesRejectedEntity);
        Assert.Equal(patchSnapshot, Snapshot(valuesRejectedResult.Entity.Patch));
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

        var entity = MakeEntity(Envelope("""{ "size": 9 }"""));
        new EntityResolver(registry, catalog).Resolve(entity);

        var call = Assert.Single(catalog.ValidateCalls);
        Assert.Equal(Type, call.Reference);

        var seen = call.Values.Read<Dictionary<string, JsonElement>>();
        Assert.Equal("9", seen["size"].GetRawText());
        Assert.NotEqual("7", seen["size"].GetRawText());
    }
}
