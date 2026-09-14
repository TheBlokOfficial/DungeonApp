using System;
using System.Collections.Generic;
using DungeonApp.Core.Events;

namespace DungeonApp.Core.Content;

/// <summary>
/// The one place a campaign's <see cref="CampaignInstance"/> records live in memory, and the only
/// way to add, remove, relabel, or patch any of them.
/// <para>
/// Everything mutable is a private <see cref="Dictionary{TKey,TValue}"/>, and the public surface is
/// exactly the operations a GM performs on the world - there is no setter, no indexer, and no
/// constructor a caller outside this class can reach once an instance exists. Every operation that
/// changes state publishes exactly one event on <see cref="CampaignEvents"/>, after the state
/// change.
/// </para>
/// </summary>
public sealed class CampaignInstances
{
    private readonly CampaignEvents _events;
    private readonly Dictionary<InstanceId, CampaignInstance> _instances = [];

    private CampaignInstances(CampaignEvents events)
    {
        _events = events;
    }

    /// <summary>Starts a campaign with no instance ever created.</summary>
    public static CampaignInstances Create(CampaignEvents events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return new CampaignInstances(events);
    }

    /// <summary>
    /// Rebuilds instance state read back from disk. Separate from <see cref="Add"/> and the other
    /// operations on purpose: loading a save is not a change anyone made this session, so it must
    /// not run through the same door a GM's action does - no event is published for any of it. It
    /// exists only as a static factory that returns a brand new instance, so there is no way to call
    /// it again on an instance that already exists.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="instances"/> names the same <see cref="InstanceId"/> more than once.
    /// </exception>
    public static CampaignInstances Hydrate(CampaignEvents events, IReadOnlyCollection<CampaignInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(instances);

        var result = new CampaignInstances(events);

        foreach (var instance in instances)
        {
            if (!result._instances.TryAdd(instance.Id, instance))
            {
                throw new ArgumentException(
                    $"Instance id '{instance.Id}' appears more than once.", nameof(instances));
            }
        }

        return result;
    }

    /// <summary>Every instance currently in the campaign, for a caller (the world view) to list.</summary>
    public IReadOnlyCollection<CampaignInstance> All => [.. _instances.Values];

    /// <summary>The instance with this id, or null when no such instance exists.</summary>
    public CampaignInstance? Find(InstanceId id) => _instances.TryGetValue(id, out var instance) ? instance : null;

    /// <summary>
    /// Brings an entry into the campaign as a new instance: a fresh <see cref="InstanceId"/>, a
    /// patch of <see cref="ContentValues.Empty"/> (a brand new instance deviates from its entry in
    /// nothing), and the given label, normalized.
    /// </summary>
    public CampaignInstance Add(EntryAddress source, string? label)
    {
        var instance = new CampaignInstance(InstanceId.New(), source, NormalizeLabel(label), ContentValues.Empty);

        _instances[instance.Id] = instance;
        _events.Publish(new InstanceAdded(instance.Id));

        return instance;
    }

    /// <summary>Removes an instance from the campaign.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="id"/> names no instance here.</exception>
    public void Remove(InstanceId id)
    {
        if (!_instances.Remove(id))
        {
            throw new InvalidOperationException($"Instance '{id}' does not exist in this campaign.");
        }

        _events.Publish(new InstanceRemoved(id));
    }

    /// <summary>Changes only the GM's own name for an instance, leaving its patch untouched.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="id"/> names no instance here.</exception>
    public void Relabel(InstanceId id, string? label)
    {
        var instance = RequireInstance(id);

        _instances[id] = instance with { Label = NormalizeLabel(label) };
        _events.Publish(new InstanceRelabelled(id));
    }

    /// <summary>Replaces only an instance's patch over its entry, leaving its label untouched.</summary>
    /// <exception cref="InvalidOperationException"><paramref name="id"/> names no instance here.</exception>
    public void ReplacePatch(InstanceId id, ContentValues patch)
    {
        ArgumentNullException.ThrowIfNull(patch);

        var instance = RequireInstance(id);

        _instances[id] = instance with { Patch = patch };
        _events.Publish(new InstancePatchReplaced(id));
    }

    private CampaignInstance RequireInstance(InstanceId id) =>
        _instances.TryGetValue(id, out var instance)
            ? instance
            : throw new InvalidOperationException($"Instance '{id}' does not exist in this campaign.");

    /// <summary>
    /// A GM's own name that is null, empty, or made entirely of whitespace carries no name at all -
    /// it collapses to <see langword="null"/> rather than being stored as a blank string a view would
    /// have to special-case.
    /// </summary>
    private static string? NormalizeLabel(string? label) =>
        string.IsNullOrWhiteSpace(label) ? null : label;
}
