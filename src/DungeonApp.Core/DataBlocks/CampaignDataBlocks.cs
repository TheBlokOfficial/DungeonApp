using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.DataBlocks;

/// <summary>
/// The one place a campaign's data block values live in memory, and the only way to change any of
/// them.
/// <para>
/// Everything mutable is a private field, and the only public members are <see cref="Read"/> and
/// <see cref="Apply"/> - there is no setter, indexer, or constructor a caller outside this class can
/// reach once an instance exists. A value can only enter here through <see cref="Apply"/>'s
/// transform, which means it is always checked against the data block's registered shape before it
/// is stored, and every write always produces exactly one <see cref="DataBlockChanged"/>
/// announcement.
/// </para>
/// <para>
/// <see cref="Read"/> never hands back the stored reference itself for anything that could be
/// mutated in place. Object-shaped values are stored as <see cref="ImmutableDictionary{TKey,TValue}"/>,
/// built fresh from whatever the transform returned, so a caller cannot reach through the returned
/// value to corrupt what is stored - and cannot do it by holding on to the object it originally
/// passed to <c>Apply</c> either, since that object was copied, never kept. Primitive values
/// (<c>string</c>, the boxed numeric types, <c>bool</c>) are immutable by construction and need no
/// copy.
/// </para>
/// </summary>
public sealed class CampaignDataBlocks
{
    private readonly DataBlockRegistry _registry;
    private readonly CampaignEvents _events;
    private readonly Dictionary<DataBlockId, object> _values = [];
    private readonly Dictionary<DataBlockId, DataBlockUnreadableReason> _unreadable = [];

    private CampaignDataBlocks(DataBlockRegistry registry, CampaignEvents events)
    {
        _registry = registry;
        _events = events;
    }

    /// <summary>Starts a campaign with no data block ever written.</summary>
    public static CampaignDataBlocks Create(DataBlockRegistry registry, CampaignEvents events)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(events);

        return new CampaignDataBlocks(registry, events);
    }

    /// <summary>
    /// Rebuilds data block state read back from disk. Separate from <see cref="Apply"/> on purpose:
    /// loading a save is not a change anyone made this session, so it must not run through the same
    /// door a GM's action does - no transform is invoked, no previous value is considered, and no
    /// <see cref="DataBlockChanged"/> is published for any of it. It exists only as a static factory
    /// that returns a brand new instance, so there is no way to call it again on an instance that
    /// already exists; the only path back into an existing <see cref="CampaignDataBlocks"/> stays
    /// <see cref="Apply"/>.
    /// <para>
    /// <paramref name="unreadable"/> names the ids the caller could not turn into a value at all - an
    /// id this registry has never heard of, or one stored at a shape version this build has no
    /// migration for. Nothing is inferred here: the persistence layer is the one that knows which
    /// case it hit and why, this method only records what it is told. An id must not appear in both
    /// <paramref name="values"/> and <paramref name="unreadable"/>.
    /// </para>
    /// </summary>
    public static CampaignDataBlocks Hydrate(
        DataBlockRegistry registry,
        CampaignEvents events,
        IReadOnlyDictionary<DataBlockId, object> values,
        IReadOnlyDictionary<DataBlockId, DataBlockUnreadableReason>? unreadable = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(values);

        var dataBlocks = new CampaignDataBlocks(registry, events);

        foreach (var (id, value) in values)
        {
            if (!registry.Knows(id))
            {
                throw new InvalidOperationException($"Data block '{id}' is not part of this build.");
            }

            var shape = registry.Describe(id).Shape;

            if (!shape.Matches(value))
            {
                throw new DataBlockShapeMismatchException(
                    id, "Stored value does not match the data block's registered shape.");
            }

            dataBlocks._values[id] = Freeze(value, shape);
        }

        foreach (var (id, reason) in unreadable ?? new Dictionary<DataBlockId, DataBlockUnreadableReason>())
        {
            if (dataBlocks._values.ContainsKey(id))
            {
                throw new ArgumentException(
                    $"Data block '{id}' is named in both {nameof(values)} and {nameof(unreadable)}.", nameof(unreadable));
            }

            dataBlocks._unreadable[id] = reason;
        }

        return dataBlocks;
    }

    /// <summary>The data block's current value, or null when nothing has ever been written to it.</summary>
    /// <exception cref="DataBlockUnreadableException">
    /// The data block has a value on disk that this build could not read back. Check
    /// <see cref="IsUnreadable"/> first to avoid the exception on an expected path.
    /// </exception>
    public object? Read(DataBlockId id)
    {
        // Checked first, ahead of the registry: an id marked unreadable because this build no longer
        // registers it must still report the same "unreadable", not fall through to "unknown build"
        // wording depending on incidental registry membership.
        if (_unreadable.TryGetValue(id, out var reason))
        {
            throw new DataBlockUnreadableException(id, reason);
        }

        if (!_registry.Knows(id))
        {
            throw new InvalidOperationException($"Data block '{id}' is not part of this build.");
        }

        return _values.TryGetValue(id, out var value) ? value : null;
    }

    /// <summary>Whether this data block survived hydration unread - see <see cref="UnreadableBlocks"/>.</summary>
    public bool IsUnreadable(DataBlockId id) => _unreadable.ContainsKey(id);

    /// <summary>
    /// Every data block this campaign could not read back, for a caller (the UI) that wants to tell
    /// the GM about it. Never thrown, never silently dropped - the campaign opens with these ids
    /// simply absent from <see cref="Read"/>'s working set.
    /// </summary>
    public IReadOnlyCollection<UnreadableDataBlock> UnreadableBlocks =>
        [.. _unreadable.Select(pair => new UnreadableDataBlock(pair.Key, pair.Value))];

    /// <summary>Every data block that currently has a value, for a caller (the store) deciding what to write.</summary>
    public IReadOnlyCollection<DataBlockId> WrittenBlocks => _values.Keys.ToArray();

    /// <summary>
    /// The only way to change a data block's value.
    /// <para>
    /// <paramref name="transform"/> receives the current value - null when the data block has never
    /// been written - and must return the new one. The result is checked against the data block's
    /// registered shape before anything is touched: a mismatch throws
    /// <see cref="DataBlockShapeMismatchException"/> and leaves the previous value exactly as it
    /// was. Only once the check passes does the new value get stored and
    /// <see cref="DataBlockChanged"/> get published on the campaign's event bus.
    /// </para>
    /// </summary>
    /// <exception cref="DataBlockUnreadableException">
    /// The data block has a value on disk that this build could not read back. Accepting a write here
    /// would replace content nobody managed to read first, which is exactly what this guards against.
    /// </exception>
    public void Apply(DataBlockId id, Func<object?, object> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        if (_unreadable.TryGetValue(id, out var unreadableReason))
        {
            throw new DataBlockUnreadableException(id, unreadableReason);
        }

        if (!_registry.Knows(id))
        {
            throw new InvalidOperationException($"Data block '{id}' is not part of this build.");
        }

        var current = _values.TryGetValue(id, out var existing) ? existing : null;
        var result = transform(current)
            ?? throw new InvalidOperationException(
                $"Data block '{id}': the transform must return a value, not null.");

        var shape = _registry.Describe(id).Shape;

        if (!shape.Matches(result))
        {
            throw new DataBlockShapeMismatchException(id, "The transform's result does not match the data block's registered shape.");
        }

        _values[id] = Freeze(result, shape);
        _events.Publish(new DataBlockChanged(id));
    }

    /// <summary>
    /// Defensively copies whatever the caller handed over so nothing kept on their side of the call
    /// can reach back into this instance's state afterwards, and - along the same walk - normalizes
    /// every primitive value to the one canonical CLR type its shape's <see cref="PrimitiveKind"/>
    /// prescribes: <see cref="PrimitiveKind.Integer"/> becomes <c>long</c>,
    /// <see cref="PrimitiveKind.Fractional"/> becomes <c>double</c>. Both are the CLR type with the
    /// widest range in their set, so normalizing to them never loses information the caller's
    /// original type could represent.
    /// <para>
    /// Without this, two writes of the same data block could leave a stored <c>int</c> in one case
    /// and a stored <c>long</c> in another - both valid under the shape, but different CLR types -
    /// and <see cref="Read"/> would hand back whichever one happened to be stored, forcing every
    /// caller to guess. Normalizing here means the value <see cref="Read"/> returns always has the
    /// same CLR type for a given shape, regardless of what the caller passed in or which path
    /// (<see cref="Apply"/> or <see cref="Hydrate"/>) produced it.
    /// </para>
    /// <para>
    /// This is always called after <c>shape.Matches</c> has already accepted the value, never in
    /// place of that check - a value that failed the shape check never reaches here, so a text value
    /// on a numeric field is still rejected, not coerced.
    /// </para>
    /// </summary>
    private static object Freeze(object value, DataBlockShape shape)
    {
        if (shape is ObjectShape objectShape && value is IReadOnlyDictionary<string, object> dictionary)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();

            foreach (var field in objectShape.Fields)
            {
                builder[field.Name] = Freeze(dictionary[field.Name], field.Type);
            }

            return builder.ToImmutable();
        }

        if (shape is PrimitiveShape { Kind: PrimitiveKind.Integer })
        {
            return Convert.ToInt64(value);
        }

        if (shape is PrimitiveShape { Kind: PrimitiveKind.Fractional })
        {
            return Convert.ToDouble(value);
        }

        return value;
    }
}
