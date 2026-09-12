using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The <see cref="IContentTypeCatalog"/> the composition root hands to <see cref="ContentPackLoader"/>:
/// looks up the owning content set by <see cref="ContentTypeReference.Set"/>, then asks that set
/// directly. docs/architecture.md's "Narzędzia biurka i system okien" has the root hold a list of
/// content sets, never a single one, so this is what fills the loader's single-catalog constructor
/// slot without the loader (or Core) ever learning that more than one content set could exist.
/// <para>
/// <see cref="HasSet"/> answers from this aggregate's own list of installed sets, independent of
/// whether any of them declares the type a given entry names - which is what lets <see
/// cref="EntryUnresolvedReason.MissingSet"/> and <see cref="EntryUnresolvedReason.MissingType"/>
/// stay two distinct, both-reachable outcomes instead of collapsing into one.
/// </para>
/// </summary>
public sealed class ContentTypeCatalogAggregate(IReadOnlyList<IContentSet> sets) : IContentTypeCatalog
{
    public bool HasSet(ContentId set) => sets.Any(candidate => candidate.Id == set);

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        var set = sets.FirstOrDefault(candidate => candidate.Id == reference.Set);

        if (set is null)
        {
            descriptor = default;
            return false;
        }

        return set.TryGet(reference, out descriptor);
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        var set = sets.FirstOrDefault(candidate => candidate.Id == reference.Set);

        if (set is null)
        {
            error = $"content set '{reference.Set}' is not installed.";
            return false;
        }

        return set.TryValidate(reference, values, out error);
    }
}
