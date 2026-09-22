using System;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Content.Instances;

/// <summary>
/// Builds the <see cref="CampaignChange"/> for each thing a GM does to an instance - what used to
/// be <c>CampaignInstances.Add</c>/<c>Remove</c>/<c>Relabel</c>/<c>ReplacePatch</c> before instances
/// became a state model. Pure helpers: no state, no event, nothing that touches a campaign. A
/// caller applies the returned change through the campaign's single change entry point
/// (<c>CampaignSession.ChangeAsync</c>), which is what actually looks anything up, mutates
/// anything, or saves anything.
/// <para>
/// <see cref="Relabel"/> and <see cref="ReplacePatch"/> take the instance's <em>current</em> value
/// rather than looking it up by id, because there is no live magazine left to ask - the caller
/// already has it, read a moment earlier from a <see cref="CampaignStateSnapshot"/>.
/// </para>
/// </summary>
public static class CampaignInstanceChanges
{
    /// <summary>Brings an entry into the campaign as a new instance: a fresh <see cref="InstanceId"/>, a patch of <see cref="ContentValues.Empty"/>, and the given label, normalized.</summary>
    public static CampaignChange Add(EntryAddress source, string? label)
    {
        var instance = new CampaignInstance
        {
            Id = InstanceId.New(),
            Source = source,
            Label = NormalizeLabel(label),
            Patch = ContentValues.Empty,
        };

        return new CampaignChange().Upsert(InstancesModel.Declaration, instance);
    }

    /// <summary>Removes the instance named <paramref name="id"/> from the campaign.</summary>
    public static CampaignChange Remove(InstanceId id) =>
        new CampaignChange().Delete(InstancesModel.Declaration, id.ToString());

    /// <summary>Changes only <paramref name="current"/>'s GM-given name, leaving its patch untouched.</summary>
    public static CampaignChange Relabel(CampaignInstance current, string? label)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new CampaignChange().Upsert(InstancesModel.Declaration, current with { Label = NormalizeLabel(label) });
    }

    /// <summary>Replaces only <paramref name="current"/>'s patch over its entry, leaving its label untouched.</summary>
    public static CampaignChange ReplacePatch(CampaignInstance current, ContentValues patch)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(patch);

        return new CampaignChange().Upsert(InstancesModel.Declaration, current with { Patch = patch });
    }

    /// <summary>
    /// A GM's own name that is null, empty, or made entirely of whitespace carries no name at all -
    /// it collapses to <see langword="null"/> rather than being stored as a blank string a view would
    /// have to special-case.
    /// </summary>
    private static string? NormalizeLabel(string? label) =>
        string.IsNullOrWhiteSpace(label) ? null : label;
}
