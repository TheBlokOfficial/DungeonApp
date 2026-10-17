using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.State;
using DungeonApp.Core.Tests.Entries.Fakes;

namespace DungeonApp.Core.Tests.Entries.Content;

/// <summary>
/// The frame's half of the live part: base plus modifiers, the deviation, the default current number,
/// the preview text and the changes it builds. The rules are a stand-in with plain arithmetic - the
/// real book arithmetic is the system's and is tested there.
/// </summary>
public sealed class EntityLivePartTests
{
    private static readonly ContentId PackId = ContentId.Create("bestiary");
    private static readonly EntryAddress Address = new(PackId, ContentId.Create("sample"));
    private static readonly ContentTypeReference Type = new(ContentId.Create("sample-set"), ContentId.Create("sample-type"));
    private static readonly ContentTypeReference OtherType = new(ContentId.Create("sample-set"), ContentId.Create("other-type"));

    private static ContentValues Envelope(string json) =>
        ContentValues.From(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!);

    private static CampaignEntity MakeEntity(CurrentValues? current = null, string? note = null) => new()
    {
        Id = EntityId.New(),
        Source = Address,
        Number = 1,
        Patch = ContentValues.Empty,
        Current = current,
        Note = note,
    };

    private static ResolvedEntity Resolve(CampaignEntity entity, ContentTypeReference? type = null)
    {
        var entryType = type ?? Type;
        var entry = new Entry(Address.Entry, "Próbka", entryType, TypeVersion: 1, Envelope("""{ "guard": 12, "poolMax": 10 }"""));
        var descriptor = new ContentTypeDescriptor(entryType, "Sample", 1);
        var registered = RegisteredEntry.CreateResolved(Address, entry, descriptor);
        var registry = new ContentRegistry([new Pack(PackId, "Bestiary", new PackVersion(1, 0), [entry])], [registered], [], []);

        return new EntityResolver(registry, FakeContentTypeCatalog.Of(descriptor)).Resolve(entity);
    }

    [Fact]
    public void A_value_without_modifiers_shows_its_base_with_no_offset()
    {
        var live = EntityLivePart.Read(Resolve(MakeEntity()), new SampleCatalog());

        var guard = live.FindValue("guard")!;
        Assert.Equal(12, guard.Base);
        Assert.Equal(12, guard.Shown);
        Assert.Equal(0, guard.Offset);
        Assert.Equal(["guard", "poolMax"], live.Values.Select(value => value.Key));
    }

    [Fact]
    public void Modifiers_add_to_their_own_value_by_key_and_the_offset_is_their_sum()
    {
        var live = EntityLivePart.Read(
            Resolve(MakeEntity()), new SampleCatalog(),
            [new ValueModifier("guard", 2), new ValueModifier("guard", 1), new ValueModifier("poolMax", -4), new ValueModifier("unknown", 9)]);

        Assert.Equal(15, live.FindValue("guard")!.Shown);
        Assert.Equal(3, live.FindValue("guard")!.Offset);
        Assert.Equal(6, live.FindValue("poolMax")!.Shown);
        Assert.Equal(-4, live.FindValue("poolMax")!.Offset);
    }

    [Fact]
    public void An_untouched_tracked_value_takes_the_default_against_the_shown_maximum()
    {
        var live = EntityLivePart.Read(Resolve(MakeEntity()), new SampleCatalog(), [new ValueModifier("poolMax", 5)]);

        var pool = live.FindTracked("pool")!;
        Assert.Equal(15, pool.Current);
        Assert.Equal(15, pool.Maximum);
        Assert.False(pool.IsChanged);
    }

    [Fact]
    public void A_stored_current_number_wins_over_the_default()
    {
        var live = EntityLivePart.Read(Resolve(MakeEntity(CurrentValues.Empty.With("pool", 3))), new SampleCatalog());

        var pool = live.FindTracked("pool")!;
        Assert.Equal(3, pool.Current);
        Assert.Equal(10, pool.Maximum);
        Assert.True(pool.IsChanged);
    }

    [Fact]
    public void A_change_stores_the_rules_result_and_leaves_everything_else_alone()
    {
        var entity = MakeEntity(note: "Kuleje.");
        var live = EntityLivePart.Read(Resolve(entity), new SampleCatalog());

        var snapshot = CampaignStateSnapshot.Empty.Apply(live.Change("pool", new NumberChange(NumberChangeKind.Subtract, 4)));
        var stored = snapshot.Get(EntitiesModel.Declaration)[entity.Id.ToString()];

        Assert.Equal(6, stored.Current!["pool"]);
        Assert.Equal("Kuleje.", stored.Note);
        Assert.Equal(entity with { Current = stored.Current }, stored);
    }

    [Fact]
    public void Changing_an_unknown_tracked_value_is_refused()
    {
        var live = EntityLivePart.Read(Resolve(MakeEntity()), new SampleCatalog());

        Assert.Throws<ArgumentException>(() => live.Change("missing", new NumberChange(NumberChangeKind.Set, 1)));
    }

    [Theory]
    [InlineData("Wykrwawia się.\nDruga linia.", "Wykrwawia się.\nDruga linia.")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Setting_the_note_keeps_its_text_and_blank_text_removes_it(string? note, string? expected)
    {
        var entity = MakeEntity(note: "Stara.");
        var live = EntityLivePart.Read(Resolve(entity), new SampleCatalog());

        var stored = CampaignStateSnapshot.Empty.Apply(live.SetNote(note)).Get(EntitiesModel.Declaration)[entity.Id.ToString()];

        Assert.Equal(expected, stored.Note);
    }

    [Fact]
    public void An_entity_without_its_entry_has_its_note_and_no_numbers()
    {
        var entity = MakeEntity(CurrentValues.Empty.With("pool", 3), note: "Zostaje.") with
        {
            Source = new EntryAddress(ContentId.Create("gone"), ContentId.Create("gone")),
        };

        var live = EntityLivePart.Read(Resolve(entity), new SampleCatalog());

        Assert.Equal("Zostaje.", live.Note);
        Assert.Empty(live.Values);
        Assert.Empty(live.Tracked);
    }

    [Fact]
    public void A_type_the_system_gives_no_rules_has_no_numbers()
    {
        var live = EntityLivePart.Read(Resolve(MakeEntity(), OtherType), new SampleCatalog());

        Assert.Empty(live.Values);
        Assert.Empty(live.Tracked);
    }

    [Theory]
    [InlineData(8, 3, 0, "8 → 3")]
    [InlineData(3, 0, 5, "3 → 0 · 5")]
    [InlineData(2, -4, 0, "2 → −4")]
    public void The_preview_shows_from_and_to_and_any_excess(int from, int to, int excess, string expected) =>
        Assert.Equal(expected, EntityLivePart.FormatPreview(from, new TrackedChangeResult(to, excess)));

    [Fact]
    public void Current_values_are_equal_by_content()
    {
        Assert.Equal(CurrentValues.Empty.With("a", 1).With("b", 2), CurrentValues.Empty.With("b", 2).With("a", 1));
        Assert.NotEqual(CurrentValues.Empty.With("a", 1), CurrentValues.Empty.With("a", 2));
    }

    private sealed class SampleCatalog : IEntityValueCatalog
    {
        public IEntityValueRules? For(ContentTypeReference type) => type == Type ? new SampleRules() : null;
    }

    private sealed class SampleRules : IEntityValueRules
    {
        public IReadOnlyList<TrackedValueRule> TrackedValues { get; } = [new PlainRule()];

        public IReadOnlyList<BaseValue> BaseValues(ContentValues values)
        {
            var read = values.Read<Dictionary<string, JsonElement>>();
            return [new BaseValue("guard", "Osłona", read["guard"].GetInt32()), new BaseValue("poolMax", "Zapas maks.", read["poolMax"].GetInt32())];
        }
    }

    private sealed class PlainRule() : TrackedValueRule("pool", "Zapas", "poolMax")
    {
        public override TrackedChangeResult Apply(int current, int maximum, NumberChange change) => change.Kind switch
        {
            NumberChangeKind.Subtract => new TrackedChangeResult(current - change.Amount, 0),
            NumberChangeKind.Add => new TrackedChangeResult(current + change.Amount, 0),
            _ => new TrackedChangeResult(change.Amount, 0),
        };
    }
}
