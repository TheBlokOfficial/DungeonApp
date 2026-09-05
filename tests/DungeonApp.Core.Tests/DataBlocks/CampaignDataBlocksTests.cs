using System;
using System.Collections.Generic;
using DungeonApp.Core.Events;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tests.DataBlocks;

public sealed class CampaignDataBlocksTests
{
    private static readonly DataBlockId RosterId = DataBlockId.Create("party.roster");
    private static readonly DataBlockId NoteId = DataBlockId.Create("party.note");

    private static readonly ObjectShape RosterShape = new(
    [
        new FieldShape("name", new PrimitiveShape(PrimitiveKind.Text)),
        new FieldShape("level", new PrimitiveShape(PrimitiveKind.Integer)),
    ]);

    private static readonly PrimitiveShape NoteShape = new(PrimitiveKind.Text);

    private static readonly DataBlockId ScoreId = DataBlockId.Create("party.score");
    private static readonly PrimitiveShape ScoreShape = new(PrimitiveKind.Fractional);

    private readonly DataBlockRegistry _registry = new DataBlockRegistry()
        .Register(RosterId, 1, RosterShape)
        .Register(NoteId, 1, NoteShape)
        .Register(ScoreId, 1, ScoreShape);

    private readonly CampaignEvents _events = new();

    private CampaignDataBlocks MakeDataBlocks() => CampaignDataBlocks.Create(_registry, _events);

    [Fact]
    public void Read_on_a_data_block_never_written_returns_null()
        => Assert.Null(MakeDataBlocks().Read(RosterId));

    [Fact]
    public void Apply_on_a_data_block_never_written_offers_the_transform_null()
    {
        var dataBlocks = MakeDataBlocks();
        object? seen = "not yet called";

        dataBlocks.Apply(RosterId, current =>
        {
            seen = current;
            return new Dictionary<string, object> { ["name"] = "Aria", ["level"] = 1 };
        });

        Assert.Null(seen);
    }

    [Fact]
    public void Apply_with_a_result_matching_the_shape_is_read_back()
    {
        var dataBlocks = MakeDataBlocks();

        dataBlocks.Apply(NoteId, _ => "Remember the amulet.");

        Assert.Equal("Remember the amulet.", dataBlocks.Read(NoteId));
    }

    [Fact]
    public void Apply_with_a_result_breaking_the_shape_throws_and_leaves_the_previous_value_untouched()
    {
        var dataBlocks = MakeDataBlocks();
        dataBlocks.Apply(NoteId, _ => "Original note.");

        Assert.Throws<DataBlockShapeMismatchException>(() => dataBlocks.Apply(NoteId, _ => 42));

        Assert.Equal("Original note.", dataBlocks.Read(NoteId));
    }

    [Fact]
    public void Apply_on_a_data_block_the_registry_does_not_know_throws()
        => Assert.Throws<InvalidOperationException>(
            () => MakeDataBlocks().Apply(DataBlockId.Create("party.unknown"), _ => "value"));

    [Fact]
    public void Apply_with_a_transform_returning_null_throws()
        => Assert.Throws<InvalidOperationException>(
            () => MakeDataBlocks().Apply(NoteId, _ => null!));

    [Fact]
    public void Apply_publishes_exactly_one_DataBlockChanged_with_the_right_id()
    {
        var dataBlocks = MakeDataBlocks();
        var heard = new List<DataBlockId>();
        _events.Subscribe<DataBlockChanged>(changed => heard.Add(changed.Id));

        dataBlocks.Apply(NoteId, _ => "A note.");

        Assert.Equal([NoteId], heard);
    }

    [Fact]
    public void Changes_to_two_different_data_blocks_publish_events_with_different_ids()
    {
        var dataBlocks = MakeDataBlocks();
        var heard = new List<DataBlockId>();
        _events.Subscribe<DataBlockChanged>(changed => heard.Add(changed.Id));

        dataBlocks.Apply(NoteId, _ => "A note.");
        dataBlocks.Apply(RosterId, _ => new Dictionary<string, object> { ["name"] = "Aria", ["level"] = 1 });

        Assert.Equal([NoteId, RosterId], heard);
    }

    /// <summary>
    /// Neither holding on to the object handed to <c>Apply</c> nor reaching through what <c>Read</c>
    /// hands back gives a caller a way to change stored state outside <c>Apply</c>.
    /// </summary>
    [Fact]
    public void Read_does_not_expose_a_value_that_can_be_mutated_in_place()
    {
        var dataBlocks = MakeDataBlocks();
        var original = new Dictionary<string, object> { ["name"] = "Aria", ["level"] = 1 };

        dataBlocks.Apply(RosterId, _ => original);

        // Mutating the object that was originally handed to Apply must not reach stored state.
        original["name"] = "Tampered";

        var stored = (IReadOnlyDictionary<string, object>)dataBlocks.Read(RosterId)!;
        Assert.Equal("Aria", stored["name"]);

        // The returned value itself refuses to be mutated.
        var asMutable = Assert.IsAssignableFrom<IDictionary<string, object>>(stored);
        Assert.Throws<NotSupportedException>(() => asMutable["name"] = "Tampered again");

        var stillStored = (IReadOnlyDictionary<string, object>)dataBlocks.Read(RosterId)!;
        Assert.Equal("Aria", stillStored["name"]);
    }

    [Fact]
    public void Apply_with_a_fractional_value_where_the_field_is_integer_throws()
    {
        var dataBlocks = MakeDataBlocks();

        Assert.Throws<DataBlockShapeMismatchException>(() => dataBlocks.Apply(
            RosterId, _ => new Dictionary<string, object> { ["name"] = "Aria", ["level"] = 1.5 }));
    }

    [Fact]
    public void Apply_with_an_integer_value_where_the_field_is_fractional_throws()
        => Assert.Throws<DataBlockShapeMismatchException>(() => MakeDataBlocks().Apply(ScoreId, _ => 7));

    [Theory]
    [InlineData((sbyte)7)]
    [InlineData((byte)7)]
    [InlineData((short)7)]
    [InlineData((ushort)7)]
    [InlineData(7)]
    [InlineData(7u)]
    [InlineData(7L)]
    [InlineData(7ul)]
    public void Apply_normalizes_any_accepted_integral_type_to_the_canonical_long(object candidate)
    {
        var dataBlocks = MakeDataBlocks();

        dataBlocks.Apply(RosterId, _ => new Dictionary<string, object> { ["name"] = "Aria", ["level"] = candidate });

        var stored = (IReadOnlyDictionary<string, object>)dataBlocks.Read(RosterId)!;
        Assert.IsType<long>(stored["level"]);
        Assert.Equal(7L, stored["level"]);
    }

    [Theory]
    [InlineData(3f)]
    [InlineData(3d)]
    public void Apply_normalizes_any_accepted_fractional_type_to_the_canonical_double(object candidate)
    {
        var dataBlocks = MakeDataBlocks();

        dataBlocks.Apply(ScoreId, _ => candidate);

        Assert.IsType<double>(dataBlocks.Read(ScoreId));
        Assert.Equal(3d, dataBlocks.Read(ScoreId));
    }

    [Fact]
    public void Apply_normalizes_decimal_to_the_canonical_double()
    {
        var dataBlocks = MakeDataBlocks();

        dataBlocks.Apply(ScoreId, _ => 3m);

        Assert.IsType<double>(dataBlocks.Read(ScoreId));
        Assert.Equal(3d, dataBlocks.Read(ScoreId));
    }

    [Fact]
    public void Apply_with_text_in_a_numeric_field_is_rejected_not_converted()
        => Assert.Throws<DataBlockShapeMismatchException>(() => MakeDataBlocks().Apply(ScoreId, _ => "3"));

    [Fact]
    public void Hydrate_normalizes_an_accepted_integral_type_to_the_canonical_long()
    {
        var dataBlocks = CampaignDataBlocks.Hydrate(
            _registry,
            _events,
            new Dictionary<DataBlockId, object>
            {
                [RosterId] = new Dictionary<string, object> { ["name"] = "Aria", ["level"] = (short)7 },
            });

        var stored = (IReadOnlyDictionary<string, object>)dataBlocks.Read(RosterId)!;
        Assert.IsType<long>(stored["level"]);
        Assert.Equal(7L, stored["level"]);
    }

    [Fact]
    public void Hydrate_normalizes_an_accepted_fractional_type_to_the_canonical_double()
    {
        var dataBlocks = CampaignDataBlocks.Hydrate(
            _registry,
            _events,
            new Dictionary<DataBlockId, object> { [ScoreId] = 3f });

        Assert.IsType<double>(dataBlocks.Read(ScoreId));
        Assert.Equal(3d, dataBlocks.Read(ScoreId));
    }

    [Fact]
    public void Hydrate_with_text_in_a_numeric_field_throws_instead_of_converting()
        => Assert.Throws<DataBlockShapeMismatchException>(() => CampaignDataBlocks.Hydrate(
            _registry,
            _events,
            new Dictionary<DataBlockId, object> { [ScoreId] = "3" }));

    private CampaignDataBlocks MakeDataBlocksWithUnreadable(
        DataBlockId id, DataBlockUnreadableReason reason = DataBlockUnreadableReason.UnsupportedVersion)
        => CampaignDataBlocks.Hydrate(
            _registry,
            _events,
            values: new Dictionary<DataBlockId, object>(),
            unreadable: new Dictionary<DataBlockId, DataBlockUnreadableReason> { [id] = reason });

    [Fact]
    public void A_data_block_named_as_unreadable_is_reported_as_such()
    {
        var dataBlocks = MakeDataBlocksWithUnreadable(NoteId, DataBlockUnreadableReason.UnsupportedVersion);

        Assert.True(dataBlocks.IsUnreadable(NoteId));
        var reported = Assert.Single(dataBlocks.UnreadableBlocks);
        Assert.Equal(NoteId, reported.Id);
        Assert.Equal(DataBlockUnreadableReason.UnsupportedVersion, reported.Reason);
    }

    /// <summary>
    /// Read must not collapse "unreadable" into "never written": both would otherwise show up to a
    /// caller as null, and the first write through Apply would then quietly overwrite content nobody
    /// managed to read first.
    /// </summary>
    [Fact]
    public void Read_on_an_unreadable_data_block_throws_instead_of_pretending_it_is_empty()
    {
        var dataBlocks = MakeDataBlocksWithUnreadable(NoteId);

        var exception = Assert.Throws<DataBlockUnreadableException>(() => dataBlocks.Read(NoteId));

        Assert.Equal(NoteId, exception.Id);
    }

    [Fact]
    public void Apply_on_an_unreadable_data_block_is_rejected()
    {
        var dataBlocks = MakeDataBlocksWithUnreadable(NoteId);

        Assert.Throws<DataBlockUnreadableException>(() => dataBlocks.Apply(NoteId, _ => "New note."));
    }

    [Fact]
    public void A_data_block_that_was_never_written_is_not_reported_as_unreadable()
    {
        var dataBlocks = MakeDataBlocks();

        Assert.False(dataBlocks.IsUnreadable(NoteId));
        Assert.Null(dataBlocks.Read(NoteId));
        Assert.Empty(dataBlocks.UnreadableBlocks);
    }

    [Fact]
    public void Hydrate_refuses_an_id_named_both_readable_and_unreadable()
        => Assert.Throws<ArgumentException>(() => CampaignDataBlocks.Hydrate(
            _registry,
            _events,
            values: new Dictionary<DataBlockId, object> { [NoteId] = "A note." },
            unreadable: new Dictionary<DataBlockId, DataBlockUnreadableReason>
            {
                [NoteId] = DataBlockUnreadableReason.UnsupportedVersion,
            }));

    [Fact]
    public void WrittenBlocks_lists_exactly_the_ids_that_have_a_value()
    {
        var dataBlocks = MakeDataBlocks();
        dataBlocks.Apply(NoteId, _ => "A note.");

        Assert.Equal([NoteId], dataBlocks.WrittenBlocks);
    }
}
