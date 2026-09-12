using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// A minimal <see cref="IContentTypeCatalog"/> for <see cref="ContentPackLoader"/> tests: knows
/// exactly the descriptors it is given, and accepts any values for them unless the test asked for a
/// specific reference's values to be refused. It exists to exercise the loader's resolution logic,
/// not any particular content type's shape - so, deliberately, it never names one. The vocabulary
/// scan (<c>CoreEntryKindIndependenceTests</c>) does not reach test projects, but there is no reason
/// to name a content type here regardless.
/// </summary>
internal sealed class FakeContentTypeCatalog(
    IReadOnlyList<ContentTypeDescriptor> descriptors,
    IReadOnlySet<ContentTypeReference>? valuesToReject = null) : IContentTypeCatalog
{
    // A "known set" is derived from the descriptors this fake was given, not tracked separately -
    // every descriptor already names the set it belongs to, so a test that wants "set installed,
    // type unknown" just registers a descriptor for a sibling type in the same set.
    public bool HasSet(ContentId set) => descriptors.Any(candidate => candidate.Reference.Set == set);

    public bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor)
    {
        foreach (var candidate in descriptors)
        {
            if (candidate.Reference == reference)
            {
                descriptor = candidate;
                return true;
            }
        }

        descriptor = default;
        return false;
    }

    public bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error)
    {
        if (!TryGet(reference, out _))
        {
            error = "no such content type is known to this fake.";
            return false;
        }

        if (valuesToReject?.Contains(reference) == true)
        {
            error = "rejected by test fake.";
            return false;
        }

        error = null;
        return true;
    }

    public static FakeContentTypeCatalog Empty() => new([]);

    public static FakeContentTypeCatalog Of(params ContentTypeDescriptor[] descriptors) => new([.. descriptors]);
}
