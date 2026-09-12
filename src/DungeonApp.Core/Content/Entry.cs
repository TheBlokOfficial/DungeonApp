namespace DungeonApp.Core.Content;

/// <summary>
/// One piece of content from a content pack: the essence docs/architecture.md's "Wpis, dokument,
/// instancja, nakładka" section calls a "wpis" - a fixed set of values, never a specific
/// in-campaign occurrence of them (that is an instance, which does not exist in this layer at
/// all).
/// <para>
/// <see cref="TypeVersion"/> is the version of the content type this entry was written against,
/// captured at file-write time rather than re-read from the content set on every load - it is
/// exactly what lets a content type's version bump leave this entry's binding provably stale
/// (<see cref="EntryUnresolvedReason.TypeVersionMismatch"/>) instead of silently reinterpreting old
/// values under a new shape.
/// </para>
/// <para>
/// <see cref="Values"/> is the unopened envelope docs/architecture.md's "Deklaracja treści" section
/// describes: the engine carries it, but only the content set named by <see cref="Type"/> ever
/// opens it, by deserializing it into its own record.
/// </para>
/// </summary>
public sealed record Entry(
    ContentId Id,
    string Name,
    ContentTypeReference Type,
    int TypeVersion,
    ContentValues Values);
