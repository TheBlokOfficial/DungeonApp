namespace DungeonApp.Core.Content;

/// <summary>
/// Why resolving a <see cref="CampaignInstance"/> against the registry did not reach its content.
/// A distinct concern from <see cref="EntryUnresolvedReason"/>: that one explains why an entry
/// never bound to a content type in the first place; this one explains why an instance - which
/// starts from an address, not from an entry already in hand - never reached one.
/// </summary>
public enum InstanceUnresolvedReason
{
    /// <summary>No installed pack answers to the id in the instance's <see cref="EntryAddress"/>.</summary>
    MissingPack,

    /// <summary>The pack is installed, but declares no entry with the given id.</summary>
    MissingEntry,

    /// <summary>
    /// The entry exists, but the registry itself never bound it to a content type - see
    /// <see cref="RegisteredEntry.Unresolved"/>. There is nothing here to overlay the patch onto.
    /// </summary>
    EntryUnresolved,

    /// <summary>
    /// The entry's values, with the instance's patch overlaid, were refused by the content set -
    /// see <see cref="ResolvedInstance.UnresolvedDetail"/> for its explanation.
    /// </summary>
    ValuesRejected
}
