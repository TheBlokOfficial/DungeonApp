namespace DungeonApp.Core.Content;

/// <summary>
/// Why a <see cref="RegisteredEntry"/> could not be bound to a content type. None of these throw a
/// pack out of the registry: the entry's own pack loaded and validated cleanly, and the problem is
/// entirely on the other side of the pack-to-content-set boundary - a content set that does not
/// know this reference at all, one that knows it only at a different version, or one that knows it
/// but refused the values this entry supplied.
/// </summary>
public enum EntryUnresolvedReason
{
    /// <summary>
    /// No installed content set resolves this entry's content type reference. <see
    /// cref="IContentTypeCatalog.TryGet"/>'s two-method contract cannot tell "no content set uses
    /// this id" apart from "that content set exists but does not declare this type" without the
    /// engine learning which content sets exist - which it must never do - so both collapse here.
    /// </summary>
    MissingSet,

    /// <summary>
    /// Reserved for a future, more capable catalog able to draw the distinction
    /// <see cref="MissingSet"/>'s remarks describe. Today's <see cref="IContentTypeCatalog"/> cannot
    /// produce it - see that type's remarks - so <see cref="ContentPackLoader"/> never returns it.
    /// </summary>
    MissingType,

    /// <summary>The content type exists, but its current version does not match the entry's declared version.</summary>
    TypeVersionMismatch,

    /// <summary>
    /// The content set knows this content type and version, but refused the entry's values -
    /// see <see cref="RegisteredEntry.UnresolvedDetail"/> for its explanation.
    /// </summary>
    ValuesRejected
}
