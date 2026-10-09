using System;
using DungeonApp.Core.State;

namespace DungeonApp.Core.Entries.Entities;

/// <summary>
/// Builds the <see cref="CampaignChange"/> for each thing a GM does to an entity - adding,
/// removing, relabelling, replacing its patch. Pure helpers: no state, no event, nothing that
/// touches a campaign. A caller applies the returned change through the campaign's single change
/// entry point (<c>CampaignSession.ChangeAsync</c>), which is what actually looks anything up,
/// mutates anything, or saves anything.
/// <para>
/// <see cref="Relabel"/> and <see cref="ReplacePatch"/> take the entity's <em>current</em> value
/// rather than looking it up by id, because there is no live collection to ask - the caller
/// already has it, read a moment earlier from a <see cref="CampaignStateSnapshot"/>.
/// </para>
/// </summary>
public static class CampaignEntityChanges
{
    /// <summary>Brings an entry into the campaign as a new entity: a fresh <see cref="EntityId"/>, a patch of <see cref="ContentValues.Empty"/>, and the given label, normalized.</summary>
    public static CampaignChange Add(EntryAddress source, string? label)
    {
        var entity = new CampaignEntity
        {
            Id = EntityId.New(),
            Source = source,
            Label = NormalizeLabel(label),
            Patch = ContentValues.Empty,
        };

        return new CampaignChange().Upsert(EntitiesModel.Declaration, entity);
    }

    /// <summary>Removes the entity named <paramref name="id"/> from the campaign.</summary>
    public static CampaignChange Remove(EntityId id) =>
        new CampaignChange().Delete(EntitiesModel.Declaration, id.ToString());

    /// <summary>Changes only <paramref name="current"/>'s GM-given name, leaving its patch untouched.</summary>
    public static CampaignChange Relabel(CampaignEntity current, string? label)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new CampaignChange().Upsert(EntitiesModel.Declaration, current with { Label = NormalizeLabel(label) });
    }

    /// <summary>Replaces only <paramref name="current"/>'s patch over its entry, leaving its label untouched.</summary>
    public static CampaignChange ReplacePatch(CampaignEntity current, ContentValues patch)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(patch);

        return new CampaignChange().Upsert(EntitiesModel.Declaration, current with { Patch = patch });
    }

    /// <summary>
    /// A GM's own name that is null, empty, or made entirely of whitespace carries no name at all -
    /// it collapses to <see langword="null"/> rather than being stored as a blank string a view would
    /// have to special-case.
    /// </summary>
    private static string? NormalizeLabel(string? label) =>
        string.IsNullOrWhiteSpace(label) ? null : label;
}
