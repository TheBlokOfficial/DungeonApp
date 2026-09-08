using System;

namespace DungeonApp.Core.Content;

/// <summary>
/// An entry's pointer to the template that gives it its shape, written in a pack file as
/// <c>"pack:template"</c> (for example <c>"dnd5e:humanoid"</c>).
/// <para>
/// Parsing is deliberately strict about the punctuation: exactly one colon, and both halves must
/// themselves be valid <see cref="ContentId"/> values. Because <see cref="ContentId"/>'s charset
/// excludes the colon, a string with more than one colon can never be misread as "the first half is
/// a slightly odd id" - it is simply rejected.
/// </para>
/// </summary>
public readonly record struct TemplateReference(ContentId Pack, ContentId Template)
{
    public static bool TryParse(string? candidate, out TemplateReference reference)
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

        var packPart = candidate[..separator];
        var templatePart = candidate[(separator + 1)..];

        if (!ContentId.TryCreate(packPart, out var pack) || !ContentId.TryCreate(templatePart, out var template))
        {
            return false;
        }

        reference = new TemplateReference(pack, template);

        return true;
    }

    public static TemplateReference Parse(string? candidate) =>
        TryParse(candidate, out var reference)
            ? reference
            : throw new ArgumentException($"Invalid template reference: '{candidate}'.", nameof(candidate));

    public override string ToString() => $"{Pack}:{Template}";
}
