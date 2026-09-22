using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The <see cref="IContentTypeCatalog"/> the composition root hands to <see cref="ContentPackLoader"/>:
/// looks up the owning system by <see cref="ContentTypeReference.Set"/>, then asks that set
/// directly. docs/architecture.md's "Narzędzia biurka i system okien" has the root hold a list of
/// systems, never a single one, so this is what fills the loader's single-catalog constructor
/// slot without the loader (or Core) ever learning that more than one system could exist.
/// <para>
/// <see cref="HasSet"/> answers from this aggregate's own list of installed sets, independent of
/// whether any of them declares the type a given entry names - which is what lets <see
/// cref="EntryUnresolvedReason.MissingSet"/> and <see cref="EntryUnresolvedReason.MissingType"/>
/// stay two distinct, both-reachable outcomes instead of collapsing into one.
/// </para>
/// </summary>
public sealed class ContentTypeCatalogAggregate(IReadOnlyList<IGameSystem> systems) : IContentTypeCatalog
{
    public bool HasSet(ContentId set) => systems.Any(candidate => candidate.Id == set);

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        var system = systems.FirstOrDefault(candidate => candidate.Id == reference.Set);

        if (system is null)
        {
            descriptor = default;
            return false;
        }

        return system.TryGet(reference, out descriptor);
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        var system = systems.FirstOrDefault(candidate => candidate.Id == reference.Set);

        if (system is null)
        {
            error = $"system '{reference.Set}' is not installed.";
            return false;
        }

        return system.TryValidate(reference, values, out error);
    }
}
