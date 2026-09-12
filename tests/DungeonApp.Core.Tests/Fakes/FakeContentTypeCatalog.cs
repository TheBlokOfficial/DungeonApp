using System.Collections.Generic;
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
