namespace DungeonApp.Core.Entries;

/// <summary>
/// Metadata about one content type a system declares: its address, its display name, and its
/// current version. Nothing here says anything about shape - the shape lives in the system's
/// own record, which this layer is never allowed to name (see <see cref="IContentTypeCatalog"/>).
/// <para>
/// A value type, deliberately: <see cref="IContentTypeCatalog.TryGet"/> hands one back through an
/// <see langword="out"/> parameter, and a value type lets a failed lookup leave that parameter at
/// its zero value instead of forcing every implementation to invent (or null-forgive) a sentinel
/// reference type instance.
/// </para>
/// <para>
/// <paramref name="ImageProperty"/> is the one piece of shape a type may declare to the engine: the
/// name of the property in its values that holds the entry's picture - a file path relative to the
/// entry's own pack (see <see cref="EntryImagePath"/>). It is a name the engine carries without knowing
/// what it means, exactly like the rest of this record; <see cref="ContentPackLoader"/> uses it to
/// check that path at load time, and <see cref="EntryImagePath.Locate"/> to find the file when the
/// picture is shown. <see langword="null"/>: the type has no picture.
/// </para>
/// </summary>
public readonly record struct ContentTypeDescriptor(
    ContentTypeReference Reference,
    string Name,
    int Version,
    string? ImageProperty = null);
