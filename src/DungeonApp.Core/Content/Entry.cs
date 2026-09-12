using System.Collections.Generic;

namespace DungeonApp.Core.Content;

/// <summary>
/// One piece of content from a content pack: the essence docs/architecture.md's "Wpis, dokument,
/// instancja, nakładka" section calls a "wpis" - a fixed set of values, never a specific
/// in-campaign occurrence of them (that is an instance, which does not exist in this layer at
/// all).
/// <para>
/// <see cref="TemplateVersion"/> is the version of <see cref="Template"/> this entry was written
/// against, captured at file-write time rather than re-read from the template on every load - it is
/// exactly what lets a template's version bump leave this entry's binding provably stale
/// (<see cref="EntryUnresolvedReason.TemplateVersionMismatch"/>) instead of silently reinterpreting
/// old values under a new shape.
/// </para>
/// <para>
/// <see cref="Values"/> holds only the fields this entry actually supplies. A declared field with no
/// value here is legal exactly when the template marked it optional; deciding what a missing
/// optional value means for rendering is not this layer's job.
/// </para>
/// </summary>
public sealed record Entry(
    ContentId Id,
    string Name,
    TemplateReference Template,
    int TemplateVersion,
    IReadOnlyDictionary<FieldName, FieldValue> Values);
