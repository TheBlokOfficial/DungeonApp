namespace DungeonApp.Core.Content;

/// <summary>
/// The narrow interface through which the engine asks content sets about content types, without
/// ever seeing a materialized content record.
/// <para>
/// <see cref="HasSet"/> answers a narrower question than <see cref="TryGet"/>: whether any
/// installed content set answers to this id at all, regardless of whether it declares the
/// particular type an entry names. Asking it does not teach the engine anything it does not
/// already hold - every entry's content type reference already carries the id of the set it
/// names (see <see cref="ContentTypeReference.Set"/>); this only asks whether that id, which the
/// engine is already carrying around, resolves to something installed. It exists so <see
/// cref="ContentPackLoader"/> can tell "no content set answers to this id" apart from "a content
/// set answers to this id, but does not declare this type" - see <see
/// cref="EntryUnresolvedReason.MissingSet"/> and <see cref="EntryUnresolvedReason.MissingType"/>.
/// </para>
/// <para>
/// <see cref="TryValidate"/> exists for one concrete reason: docs/architecture.md's "Paczki,
/// wczytywanie, bezpieczeństwo" section requires that malformed content be caught at startup, never
/// on the first click mid-session. The engine cannot check a value against a shape it does not
/// know - only the content set that declared the shape can - so <see cref="ContentPackLoader"/>
/// must ask at load time, through this method, rather than deferring the check to whatever renders
/// the entry later.
/// </para>
/// <para>
/// <see cref="TryValidate"/> deserializes the envelope internally and reports only success or
/// failure-with-a-reason; it never hands a materialized content record back to the engine, because
/// the engine has no type to receive it as. The consequence, accepted deliberately: values
/// deserialize twice - once here to validate, once more when the content set actually builds a
/// card - which at the hundreds-of-entries, single-machine scale this application targets costs
/// nothing worth avoiding. The alternative (returning the deserialized object) would hand the
/// engine an object of a type it does not know exists.
/// </para>
/// </summary>
public interface IContentTypeCatalog
{
    bool HasSet(ContentId set);

    bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor);

    bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error);
}
