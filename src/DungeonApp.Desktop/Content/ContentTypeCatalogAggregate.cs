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
/// When no content set matches <see cref="ContentTypeReference.Set"/>, or the matching set does not
/// itself know the referenced type, both collapse into <c>false</c> here - see
/// <see cref="EntryUnresolvedReason.MissingSet"/>'s remarks for why <see cref="IContentTypeCatalog"/>'s
/// two-method shape cannot let a caller tell those two apart.
/// </para>
/// </summary>
public sealed class ContentTypeCatalogAggregate(IReadOnlyList<IContentSet> sets) : IContentTypeCatalog
{
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
