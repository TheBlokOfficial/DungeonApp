using System;
using System.Buffers;
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
/// <para>
/// <see cref="Overlay"/>, <see cref="Difference"/> and <see cref="From{T}"/> are what a
/// <see cref="CampaignInstance"/>'s sparse patch is built and read through, and they stand on that
/// same argument. They move whole properties around by the names the envelope already carries; none
/// of them writes down a name, asks what a name means, or reads a value. Merging two envelopes
/// key-for-key is no more knowledge of an entry's shape than carrying one envelope unopened is -
/// which is the only reason the engine is allowed to hold a patch at all.
/// </para>
/// </summary>
public sealed class ContentValues
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>
    /// Writing an envelope back out has to mirror <see cref="Options"/>'s naming policy exactly, or
    /// every key would land next to the entry's key instead of on it and
    /// <see cref="Difference"/> would report every property as changed. The policy is read off
    /// <see cref="Options"/> rather than restated, so the two cannot drift apart later.
    /// <para>
    /// <see cref="JsonIgnoreCondition.WhenWritingNull"/> is load-bearing, not tidiness. Content
    /// records are mostly optional properties, so without it every property a record left unset
    /// would be written as an explicit null, <see cref="Difference"/> would score it as differing
    /// from an entry that simply has no such key, and the patch would overlay nulls over the
    /// entry's own values. It also buys the behaviour the patch wants: setting a property back to
    /// null drops its key from the patch, and reading falls back to the entry.
    /// </para>
    /// </summary>
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = Options.PropertyNamingPolicy,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly JsonElement _raw;

    internal ContentValues(JsonElement raw) => _raw = raw.Clone();

    /// <summary>
    /// An envelope with no properties: an instance that deviates from its entry in nothing.
    /// </summary>
    public static ContentValues Empty { get; } = CreateEmpty();

    /// <summary>
    /// Whether the envelope carries no properties at all. A store asks this to avoid persisting a
    /// patch that says nothing.
    /// </summary>
    public bool IsEmpty => !RequireObject(this, nameof(IsEmpty)).EnumerateObject().MoveNext();

    /// <summary>
    /// Deserializes the envelope into <typeparamref name="T"/>, camelCase-mapped and strict about
    /// unmapped members. Throws on any failure - a missing required member, an unknown key, a value
    /// of the wrong shape - and leaves catching it to the caller: a content set's
    /// <see cref="IContentTypeCatalog.TryValidate"/> implementation, or its card-building code,
    /// both of which know what a rejection means here, unlike this bare envelope type.
    /// </summary>
    public T Read<T>() => JsonSerializer.Deserialize<T>(_raw.GetRawText(), Options)!;

    /// <summary>
    /// Returns a new envelope holding this one's properties with <paramref name="patch"/>'s laid
    /// over them: same-named properties replaced, new ones added. Neither side is modified - this
    /// is how an instance is read without ever materializing the entry it points at.
    /// <para>
    /// The merge is <em>shallow</em>, by decision rather than by omission: if a property's value is
    /// itself an object, the patch replaces that object whole and does not merge into it. A deep
    /// merge would have to decide what a nested key means - whether two objects under the same name
    /// describe the same thing and should be combined, or are two different things and should be
    /// swapped - and that is a judgement only the content set may make. The shallow rule needs no
    /// such judgement, so it is the one the engine can hold.
    /// </para>
    /// </summary>
    public ContentValues Overlay(ContentValues patch)
    {
        var baseline = RequireObject(this, nameof(Overlay));
        var overlay = RequireObject(patch, nameof(patch));

        return Build(writer =>
        {
            foreach (var property in baseline.EnumerateObject())
            {
                if (overlay.TryGetProperty(property.Name, out var replacement))
                {
                    writer.WritePropertyName(property.Name);
                    replacement.WriteTo(writer);
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            foreach (var property in overlay.EnumerateObject())
            {
                if (!baseline.TryGetProperty(property.Name, out _))
                {
                    property.WriteTo(writer);
                }
            }
        });
    }

    /// <summary>
    /// Returns the sparse patch that turns <paramref name="baseline"/> into
    /// <paramref name="candidate"/>: every property whose value differs, plus every property
    /// <paramref name="baseline"/> does not have at all.
    /// <para>
    /// A property present in <paramref name="baseline"/> and absent from
    /// <paramref name="candidate"/> is deliberately not reported. A patch is an overlay and has no
    /// way to say "remove this key"; giving it one would make the patch able to reshape an entry
    /// rather than deviate from it.
    /// </para>
    /// <para>
    /// Values are compared with <see cref="JsonElement.DeepEquals(JsonElement, JsonElement)"/>
    /// rather than by raw text, because raw text differs on whitespace and on how the same number
    /// was written, and either would be recorded as a deviation that nobody made.
    /// </para>
    /// </summary>
    public static ContentValues Difference(ContentValues baseline, ContentValues candidate)
    {
        var original = RequireObject(baseline, nameof(baseline));
        var proposed = RequireObject(candidate, nameof(candidate));

        return Build(writer =>
        {
            foreach (var property in proposed.EnumerateObject())
            {
                var unchanged = original.TryGetProperty(property.Name, out var previous)
                                && JsonElement.DeepEquals(previous, property.Value);

                if (!unchanged)
                {
                    property.WriteTo(writer);
                }
            }
        });
    }

    /// <summary>
    /// Seals a content set's record back into an envelope - the return leg of
    /// <see cref="Read{T}"/>, and the only way an edited record becomes something
    /// <see cref="Difference"/> can compare. Like <see cref="Read{T}"/>, the engine never names a
    /// concrete <typeparamref name="T"/>; the content set that opened the envelope is the one that
    /// closes it.
    /// </summary>
    public static ContentValues From<T>(T record) =>
        new(JsonSerializer.SerializeToElement(record, WriteOptions));

    private static ContentValues CreateEmpty()
    {
        using var document = JsonDocument.Parse("{}");
        return new ContentValues(document.RootElement);
    }

    /// <summary>
    /// An envelope over anything but a JSON object cannot be merged or differenced, and should
    /// never have been built. It is not silently tolerated: a store that quietly did nothing with
    /// one would lose an edit without saying so.
    /// </summary>
    private static JsonElement RequireObject(ContentValues values, string operation)
    {
        if (values._raw.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"{operation} needs an envelope holding a JSON object, but this one holds {values._raw.ValueKind}.");
        }

        return values._raw;
    }

    private static ContentValues Build(Action<Utf8JsonWriter> writeProperties)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writeProperties(writer);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return new ContentValues(document.RootElement);
    }
}
