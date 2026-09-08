namespace DungeonApp.Core.Content;

/// <summary>
/// Why a <see cref="RegisteredEntry"/> could not be bound to its template. Distinct from a rejected
/// pack: none of these throw a pack out of the registry, because the entry's own pack loaded and
/// validated cleanly - the problem is entirely on the other side of the 3-to-2 dependency edge, in a
/// system pack that is missing, missing that template, or has moved the template on.
/// </summary>
public enum EntryUnresolvedReason
{
    /// <summary>The system pack the entry's template reference names is not installed (or was rejected).</summary>
    MissingPack,

    /// <summary>The system pack is installed, but it declares no template with the referenced id.</summary>
    MissingTemplate,

    /// <summary>The template exists, but its current <see cref="Template.Version"/> does not match the entry's.</summary>
    TemplateVersionMismatch
}
