using System;
using System.Collections.Generic;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Events;
using DungeonApp.Core.Tools.Counter;

namespace DungeonApp.Core.Tests.Tools.Counter;

public sealed class CounterToolTests
{
    private readonly CounterTool _tool = new();

    [Fact]
    public void Increment_applied_to_an_unwritten_block_produces_one()
    {
        var value = ReadCount(_tool.Increment()(null));

        Assert.Equal(1L, value);
    }

    [Fact]
    public void Increment_and_decrement_return_to_the_initial_value()
    {
        object initial = CreateValue(17L);

        var incremented = _tool.Increment()(initial);
        var decremented = _tool.Decrement()(incremented);

        Assert.Equal(17L, ReadCount(decremented));
    }

    [Fact]
    public void Uses_declares_exactly_the_counter_data_block()
        => Assert.Equal([CounterTool.DataBlockId], _tool.Uses);

    [Fact]
    public void Transform_applied_through_campaign_data_changes_the_value_and_announces_it()
    {
        var registry = new DataBlockRegistry()
            .Register(CounterTool.DataBlockId, CounterTool.DataBlockVersion, CounterTool.DataBlockShape);
        var events = new CampaignEvents();
        var announcements = new List<DataBlockChanged>();
        events.Subscribe<DataBlockChanged>(announcements.Add);
        var dataBlocks = CampaignDataBlocks.Create(registry, events);

        dataBlocks.Apply(CounterTool.DataBlockId, _tool.Increment());

        Assert.Equal(1L, ReadCount(dataBlocks.Read(CounterTool.DataBlockId)!));
        Assert.Equal([new DataBlockChanged(CounterTool.DataBlockId)], announcements);
    }

    [Fact]
    public void Reusing_transforms_through_campaign_data_uses_the_current_frozen_value_and_announces_each_change()
    {
        var registry = new DataBlockRegistry()
            .Register(CounterTool.DataBlockId, CounterTool.DataBlockVersion, CounterTool.DataBlockShape);
        var events = new CampaignEvents();
        var announcements = new List<DataBlockChanged>();
        events.Subscribe<DataBlockChanged>(announcements.Add);
        var dataBlocks = CampaignDataBlocks.Create(registry, events);
        var increment = _tool.Increment();

        dataBlocks.Apply(CounterTool.DataBlockId, increment);
        Assert.Equal(1L, ReadCount(dataBlocks.Read(CounterTool.DataBlockId)!));

        dataBlocks.Apply(CounterTool.DataBlockId, increment);
        Assert.Equal(2L, ReadCount(dataBlocks.Read(CounterTool.DataBlockId)!));

        dataBlocks.Apply(CounterTool.DataBlockId, _tool.Decrement());

        Assert.Equal(1L, ReadCount(dataBlocks.Read(CounterTool.DataBlockId)!));
        Assert.Equal(
            [
                new DataBlockChanged(CounterTool.DataBlockId),
                new DataBlockChanged(CounterTool.DataBlockId),
                new DataBlockChanged(CounterTool.DataBlockId),
            ],
            announcements);
    }

    [Theory]
    [InlineData(true, long.MaxValue)]
    [InlineData(false, long.MinValue)]
    public void Overflowing_transform_is_rejected_without_changing_campaign_data_or_announcing_a_change(
        bool increment,
        long initial)
    {
        var registry = new DataBlockRegistry()
            .Register(CounterTool.DataBlockId, CounterTool.DataBlockVersion, CounterTool.DataBlockShape);
        var events = new CampaignEvents();
        var dataBlocks = CampaignDataBlocks.Create(registry, events);
        dataBlocks.Apply(CounterTool.DataBlockId, _ => CreateValue(initial));

        var announcements = new List<DataBlockChanged>();
        events.Subscribe<DataBlockChanged>(announcements.Add);

        var transform = increment ? _tool.Increment() : _tool.Decrement();

        Assert.Throws<OverflowException>(() => dataBlocks.Apply(CounterTool.DataBlockId, transform));

        Assert.Equal(initial, ReadCount(dataBlocks.Read(CounterTool.DataBlockId)!));
        Assert.Empty(announcements);
    }

    private static object CreateValue(long count) =>
        new Dictionary<string, object> { [CounterTool.CountField] = count };

    private static long ReadCount(object value) =>
        (long)((IReadOnlyDictionary<string, object>)value)[CounterTool.CountField];
}
