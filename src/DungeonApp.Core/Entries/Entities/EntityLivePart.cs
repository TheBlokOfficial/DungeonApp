using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// A value with a base as the entity shows it: the base from the entry and the base plus the sum of
/// its modifiers. <see cref="Offset"/> is what a view colours (above or below the base).
/// </summary>
public sealed record ShownValue(string Key, string Label, int Base, int Shown)
{
    public int Offset => Shown - Base;
}

/// <summary>
/// A tracked value of one entity: its current number and the shown maximum it is measured against.
/// <see cref="IsChanged"/> tells a number the GM set from the system's default.
/// </summary>
public sealed record TrackedValue(TrackedValueRule Rule, int Current, int Maximum, bool IsChanged)
{
    public string Key => Rule.Key;

    public string Label => Rule.Label;

    /// <summary>What <paramref name="change"/> would leave, by the system's arithmetic.</summary>
    public TrackedChangeResult Apply(NumberChange change) => Rule.Apply(Current, Maximum, change);

    /// <summary>The preview under the field: "8 → 3", or "3 → 0 · 5" when 5 did not fit above the floor.</summary>
    public string Preview(NumberChange change) => EntityLivePart.FormatPreview(Current, Apply(change));
}

/// <summary>
/// The live part of one entity as the frame computes it: the note, the values with a base (base plus
/// modifiers) and the tracked values (the stored number, or the system's default against the shown
/// maximum). Read fresh from a resolved entity after every change; it holds no state of its own, and
/// what it builds (<see cref="Change"/>, <see cref="SetNote"/>) is a <see cref="CampaignChange"/> the
/// caller applies through the campaign's single change entry point, which saves it at once and tells
/// every view.
/// <para>
/// An entity whose entry is gone (or whose type has no live numbers) still has its note; its values
/// and tracked values are empty.
/// </para>
/// </summary>
public sealed class EntityLivePart
{
    private const char TypographicMinus = '−';

    private EntityLivePart(CampaignEntity entity, IReadOnlyList<ShownValue> values, IReadOnlyList<TrackedValue> tracked)
    {
        Entity = entity;
        Values = values;
        Tracked = tracked;
    }

    public CampaignEntity Entity { get; }

    public string? Note => Entity.Note;

    public IReadOnlyList<ShownValue> Values { get; }

    public IReadOnlyList<TrackedValue> Tracked { get; }

    /// <summary>
    /// Computes the live part of <paramref name="resolved"/>. <paramref name="modifiers"/> are added to
    /// the values with a base by key; none means every value shows its base.
    /// </summary>
    public static EntityLivePart Read(
        ResolvedEntity resolved, IEntityValueCatalog catalog, IReadOnlyList<ValueModifier>? modifiers = null)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        ArgumentNullException.ThrowIfNull(catalog);

        if (resolved is not { Source: { } registered, Values: { } values, Unresolved: null }
            || catalog.For(registered.Entry.Type) is not { } rules)
        {
            return new EntityLivePart(resolved.Entity, [], []);
        }

        var offsets = (modifiers ?? [])
            .GroupBy(modifier => modifier.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(modifier => modifier.Amount), StringComparer.Ordinal);

        ShownValue[] shown =
        [
            .. rules.BaseValues(values).Select(value =>
                new ShownValue(value.Key, value.Label, value.Base, value.Base + offsets.GetValueOrDefault(value.Key))),
        ];

        var stored = resolved.Entity.Current ?? CurrentValues.Empty;

        TrackedValue[] tracked =
        [
            .. rules.TrackedValues.Select(rule =>
            {
                var maximum = shown.FirstOrDefault(value => value.Key == rule.MaximumKey)
                    ?? throw new InvalidOperationException(
                        $"Tracked value '{rule.Key}' names maximum '{rule.MaximumKey}', which is not a value with a base.");

                return stored.TryGetValue(rule.Key, out var current)
                    ? new TrackedValue(rule, current, maximum.Shown, IsChanged: true)
                    : new TrackedValue(rule, rule.DefaultCurrent(maximum.Shown), maximum.Shown, IsChanged: false);
            }),
        ];

        return new EntityLivePart(resolved.Entity, shown, tracked);
    }

    /// <summary>The value with a base named <paramref name="key"/>, or <see langword="null"/>.</summary>
    public ShownValue? FindValue(string key) => Values.FirstOrDefault(value => value.Key == key);

    /// <summary>The tracked value named <paramref name="key"/>, or <see langword="null"/>.</summary>
    public TrackedValue? FindTracked(string key) => Tracked.FirstOrDefault(value => value.Key == key);

    /// <summary>
    /// The change that applies <paramref name="change"/> to the tracked value <paramref name="key"/>
    /// by the system's arithmetic. The result is stored even when it equals the default: a number the
    /// GM touched no longer follows the maximum.
    /// </summary>
    public CampaignChange Change(string key, NumberChange change)
    {
        var tracked = FindTracked(key)
            ?? throw new ArgumentException($"The entity has no tracked value '{key}'.", nameof(key));

        return CampaignEntityChanges.SetCurrent(Entity, key, tracked.Apply(change).Value);
    }

    /// <summary>The change that sets the GM's note; blank text removes it.</summary>
    public CampaignChange SetNote(string? note) => CampaignEntityChanges.SetNote(Entity, note);

    /// <summary>"8 → 3"; with an excess "3 → 0 · 5". Negative numbers carry the typographic minus.</summary>
    public static string FormatPreview(int from, TrackedChangeResult result)
    {
        var text = $"{Format(from)} → {Format(result.Value)}";
        return result.Excess > 0 ? $"{text} · {Format(result.Excess)}" : text;
    }

    private static string Format(int number) =>
        number.ToString(CultureInfo.InvariantCulture).Replace('-', TypographicMinus);
}
