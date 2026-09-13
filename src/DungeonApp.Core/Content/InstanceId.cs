using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// One instance's permanent identity. Deliberately separate from both the entry it points at and
/// the label the GM gives it: two instances of the same entry are still two different things, and
/// a relabelled instance is still the same one. It has to survive every rename and every edit to
/// the patch, so it is never derived from either.
/// </summary>
public readonly record struct InstanceId(Guid Value)
{
    public static InstanceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
