using System;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tests.DataBlocks;

public sealed class DataBlockRegistryTests
{
    private static readonly DataBlockId RosterId = DataBlockId.Create("party.roster");
    private static readonly PrimitiveShape TextShape = new(PrimitiveKind.Text);

    private readonly DataBlockRegistry _registry = new DataBlockRegistry()
        .Register(RosterId, 1, TextShape);

    [Fact]
    public void Knows_a_registered_data_block()
        => Assert.True(_registry.Knows(RosterId));

    [Fact]
    public void Does_not_know_a_data_block_it_was_never_told_about()
        => Assert.False(_registry.Knows(DataBlockId.Create("party.unknown")));

    [Fact]
    public void Describes_a_registered_data_block()
    {
        var registration = _registry.Describe(RosterId);

        Assert.Equal(RosterId, registration.Id);
        Assert.Equal(1, registration.Version);
        Assert.Equal(TextShape, registration.Shape);
    }

    [Fact]
    public void Refuses_to_describe_a_data_block_this_build_does_not_have()
        => Assert.Throws<InvalidOperationException>(
            () => _registry.Describe(DataBlockId.Create("party.unknown")));

    [Fact]
    public void Refuses_the_same_data_block_twice()
        => Assert.Throws<ArgumentException>(() => _registry.Register(RosterId, 1, TextShape));
}
