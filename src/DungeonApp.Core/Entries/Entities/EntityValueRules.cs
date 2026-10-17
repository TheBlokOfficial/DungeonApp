using System;
using System.Collections.Generic;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// Where a system tells the frame, per content type, which of an entity's numbers have a base and
/// which are tracked. A type the system returns <see langword="null"/> for has neither: its entities
/// still have a note, nothing more.
/// </summary>
public interface IEntityValueCatalog
{
    IEntityValueRules? For(ContentTypeReference type);
}

/// <summary>
/// One content type's live numbers, as its system defines them. The frame keeps the state, sums the
/// modifiers and colours the deviation; it never learns what any key means.
/// </summary>
public interface IEntityValueRules
{
    /// <summary>
    /// The values with a base, read from the entity's values (the entry's with the entity's patch
    /// overlaid), in the order the system lists them. The base is what the entry says; a value shown
    /// differently comes only from modifiers.
    /// </summary>
    IReadOnlyList<BaseValue> BaseValues(ContentValues values);

    /// <summary>The tracked values, each with the base value that is its maximum.</summary>
    IReadOnlyList<TrackedValueRule> TrackedValues { get; }
}

/// <summary>A value with a base: its key, the label the GM reads, and the base from the entry.</summary>
public sealed record BaseValue(string Key, string Label, int Base);

/// <summary>
/// One number added to a value with a base. The GM puts it there as a part of an effect; a shown value
/// is its base plus every modifier with its key.
/// </summary>
public sealed record ValueModifier(string Key, int Amount);

/// <summary>
/// What a change field's entry does to a tracked value: the value it leaves, and how much of a
/// subtraction did not fit above the floor (0 when all of it did).
/// </summary>
public readonly record struct TrackedChangeResult(int Value, int Excess);

/// <summary>
/// A tracked value - a number the GM changes at the table through a change field
/// (<see cref="NumberChange"/>). The system names it, says which value with a base is its maximum and
/// does the arithmetic of the book; the frame stores the result.
/// </summary>
public abstract class TrackedValueRule
{
    protected TrackedValueRule(string key, string label, string maximumKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(maximumKey);

        Key = key;
        Label = label;
        MaximumKey = maximumKey;
    }

    public string Key { get; }

    public string Label { get; }

    /// <summary>The <see cref="BaseValue.Key"/> of the value with a base that is this value's maximum.</summary>
    public string MaximumKey { get; }

    /// <summary>The current number of an entity the GM has not changed, given the shown maximum.</summary>
    public virtual int DefaultCurrent(int maximum) => maximum;

    /// <summary>
    /// What <paramref name="change"/> does to <paramref name="current"/> against the shown
    /// <paramref name="maximum"/>. A state against the rules (above the maximum) is allowed to come
    /// out: it shows instead of being blocked.
    /// </summary>
    public abstract TrackedChangeResult Apply(int current, int maximum, NumberChange change);
}

/// <summary>The catalog of a system with no live numbers at all.</summary>
public sealed class NoEntityValues : IEntityValueCatalog
{
    public static NoEntityValues Instance { get; } = new();

    private NoEntityValues()
    {
    }

    public IEntityValueRules? For(ContentTypeReference type) => null;
}
