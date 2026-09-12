namespace DungeonApp.Core.Content;

/// <summary>
/// Metadata about one content type a content set declares: its address, its display name, and its
/// current version. Nothing here says anything about shape - the shape lives in the content set's
/// own record, which this layer is never allowed to name (see <see cref="IContentTypeCatalog"/>).
/// <para>
/// A value type, deliberately: <see cref="IContentTypeCatalog.TryGet"/> hands one back through an
/// <see langword="out"/> parameter, and a value type lets a failed lookup leave that parameter at
/// its zero value instead of forcing every implementation to invent (or null-forgive) a sentinel
/// reference type instance.
/// </para>
/// </summary>
public readonly record struct ContentTypeDescriptor(ContentTypeReference Reference, string Name, int Version);
