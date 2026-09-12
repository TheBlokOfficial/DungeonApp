using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonApp.Core.Content;

/// <summary>
/// A sealed envelope over one entry's raw <c>values</c> object. This is the one class
/// docs/architecture.md's "Deklaracja treści" section means when it says the file format is now a
/// choice of serializer, not an architectural decision: swap the serializer here, and every content
/// set's call to <see cref="Read{T}"/> keeps compiling and behaving the same, because none of them
/// ever touch a <see cref="JsonElement"/> directly.
/// <para>
/// <see cref="Read{T}"/> is generic, and <c>Core</c> is allowed to carry a generic method here
/// without breaking the boundary that keeps the engine from ever knowing what kind of thing an
/// entry is: the engine itself never names any concrete <c>T</c>. It stores this envelope unopened - the constructor is
/// <see langword="internal"/>, so only <see cref="ContentPackLoader"/> ever builds one - and hands
/// it, still sealed, to whichever content set claims the entry's content type reference. That
/// content set is the only code that ever instantiates <see cref="Read{T}"/> with a concrete
/// record. A generic method whose type parameter the generic code itself never names is opaque to
/// that code, not a leak of the boundary it sits inside.
/// </para>
/// </summary>
public sealed class ContentValues
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly JsonElement _raw;

    internal ContentValues(JsonElement raw) => _raw = raw.Clone();

    /// <summary>
    /// Deserializes the envelope into <typeparamref name="T"/>, camelCase-mapped and strict about
    /// unmapped members. Throws on any failure - a missing required member, an unknown key, a value
    /// of the wrong shape - and leaves catching it to the caller: a content set's
    /// <see cref="IContentTypeCatalog.TryValidate"/> implementation, or its card-building code,
    /// both of which know what a rejection means here, unlike this bare envelope type.
    /// </summary>
    public T Read<T>() => JsonSerializer.Deserialize<T>(_raw.GetRawText(), Options)!;
}
