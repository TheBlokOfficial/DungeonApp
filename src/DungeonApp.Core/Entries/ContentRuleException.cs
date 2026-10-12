using System.Text.Json;

namespace DungeonApp.Core.Entries;

/// <summary>
/// A <see cref="JsonException"/> raised by a content rule (an invalid id, an empty required text, a
/// combination of fields that cannot go together) rather than by the parser. It carries the explanation
/// the GM reads as <see cref="Reason"/> - a finished Polish sentence - because the serializer appends an
/// English path suffix to the message of every <see cref="JsonException"/> that passes through it, and
/// <see cref="ContentFailureText.Describe"/> must be able to show the sentence alone.
/// </summary>
public sealed class ContentRuleException : JsonException
{
    public ContentRuleException(string reason)
        : base(reason)
    {
        Reason = reason;
    }

    /// <summary>The finished sentence, without any path suffix the serializer may have added to <c>Message</c>.</summary>
    public string Reason { get; }
}
