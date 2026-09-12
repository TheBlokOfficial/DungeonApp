using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// An entry's pointer to the content type that gives it its shape, written in a pack file as
/// <c>"set:type"</c> - the id of the content set that declares the type, then the id of the type
/// itself within that set.
/// <para>
/// Parsing is deliberately strict about the punctuation: exactly one colon, and both halves must
/// themselves be valid <see cref="ContentId"/> values. Because <see cref="ContentId"/>'s charset
/// excludes the colon, a string with more than one colon can never be misread as "the first half is
/// a slightly odd id" - it is simply rejected.
/// </para>
/// </summary>
public readonly record struct ContentTypeReference(ContentId Set, ContentId Type)
{
    public static bool TryParse(string? candidate, out ContentTypeReference reference)
    {
        reference = default;

        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        var separator = candidate.IndexOf(':');

        if (separator < 0 || candidate.IndexOf(':', separator + 1) >= 0)
        {
            return false;
        }

        var setPart = candidate[..separator];
        var typePart = candidate[(separator + 1)..];

        if (!ContentId.TryCreate(setPart, out var set) || !ContentId.TryCreate(typePart, out var type))
        {
            return false;
        }

        reference = new ContentTypeReference(set, type);

        return true;
    }

    public static ContentTypeReference Parse(string? candidate) =>
        TryParse(candidate, out var reference)
            ? reference
            : throw new ArgumentException($"Invalid content type reference: '{candidate}'.", nameof(candidate));

    public override string ToString() => $"{Set}:{Type}";
}
