using System;
using System.Collections.Generic;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tools.Counter;

public sealed class CounterTool : ITool
{
    public const int DataBlockVersion = 1;
    public const string CountField = "count";

    public static readonly DataBlockId DataBlockId = DataBlockId.Create("counter");

    public static readonly DataBlockShape DataBlockShape = new ObjectShape(
    [
        new FieldShape(CountField, new PrimitiveShape(PrimitiveKind.Integer)),
    ]);

    public IReadOnlyList<DataBlockId> Uses { get; } = [DataBlockId];

    public Func<object?, object> Increment() => current => CreateValue(checked(ReadCount(current) + 1));

    public Func<object?, object> Decrement() => current => CreateValue(checked(ReadCount(current) - 1));

    private static long ReadCount(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        var fields = (IReadOnlyDictionary<string, object>)value;
        return (long)fields[CountField];
    }

    private static object CreateValue(long count) =>
        new Dictionary<string, object> { [CountField] = count };
}
