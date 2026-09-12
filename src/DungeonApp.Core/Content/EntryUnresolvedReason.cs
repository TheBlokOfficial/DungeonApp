namespace DungeonApp.Core.Content;

/// <summary>
/// Why a <see cref="RegisteredEntry"/> could not be bound to a content type. None of these throw a
/// pack out of the registry: the entry's own pack loaded and validated cleanly, and the problem is
/// entirely on the other side of the pack-to-content-set boundary - no content set answers to the
/// id this entry names, the content set that does answer to it does not declare this type, the
/// type it declares exists only at a different version, or the content set knows the type but
/// refused the values this entry supplied.
/// </summary>
public enum EntryUnresolvedReason
{
    /// <summary>
    /// No installed content set answers to this entry's content type reference at all - see
    /// <see cref="IContentTypeCatalog.HasSet"/>.
    /// </summary>
    MissingSet,

    /// <summary>
    /// The content set this entry's content type reference names is installed, but that set
    /// declares no content type with the given id - see <see cref="IContentTypeCatalog.TryGet"/>.
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
