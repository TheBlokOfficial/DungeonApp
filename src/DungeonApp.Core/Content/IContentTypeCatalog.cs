namespace DungeonApp.Core.Content;

/// <summary>
/// The narrow interface through which the engine asks content sets about content types, without
/// ever seeing a materialized content record.
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
    bool TryGet(ContentTypeReference reference, out ContentTypeDescriptor descriptor);

    bool TryValidate(ContentTypeReference reference, ContentValues values, out string? error);
}
