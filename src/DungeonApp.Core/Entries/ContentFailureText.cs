using System;
using System.Linq;
using System.Text.Json;

namespace DungeonApp.Core.Entries;

/// <summary>
/// Turns a failure of reading content into the Polish sentence the GM sees as the reason a pack, an
/// entry or its values were rejected. The serializer's own messages are English and speak of .NET types;
/// a sentence built from the failing field's path names what to fix instead.
/// </summary>
public static class ContentFailureText
{
    /// <summary>Describes a failure of reading JSON content; a rule exception keeps its own sentence.</summary>
    public static string Describe(JsonException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is ContentRuleException rule)
        {
            return rule.Reason;
        }

        var message = exception.Message;
        var field = FieldOf(exception.Path);

        if (message.Contains("could not be mapped", StringComparison.Ordinal))
        {
            return field is null
                ? "Plik zawiera nieznane pole."
                : $"Pole `{field}` nie istnieje w tym miejscu.";
        }

        if (message.Contains("missing required properties", StringComparison.Ordinal))
        {
            var missing = MissingProperties(message);
            return missing is null ? "Brakuje wymaganego pola." : $"Brakuje wymaganego pola: {missing}.";
        }

        if (message.Contains("could not be converted", StringComparison.Ordinal))
        {
            return field is null
                ? "Zawartość pliku ma zły typ."
                : $"Pole `{field}`: wartość ma zły typ.";
        }

        return exception.LineNumber is { } line
            ? $"Plik nie jest poprawnym JSON-em (wiersz {line + 1})."
            : "Plik nie jest poprawnym JSON-em.";
    }

    // "$.values.combat.ac" -> "values.combat.ac"; the root ("$") names no field.
    private static string? FieldOf(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "$")
        {
            return null;
        }

        return path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;
    }

    // "... was missing required properties including: 'a', 'b'." -> "`a`, `b`".
    private static string? MissingProperties(string message)
    {
        var marker = message.IndexOf("including:", StringComparison.Ordinal);

        if (marker < 0)
        {
            return null;
        }

        var names = message[(marker + "including:".Length)..]
            .Split('\'')
            .Where((_, index) => index % 2 == 1)
            .Select(name => $"`{name}`")
            .ToArray();

        return names.Length == 0 ? null : string.Join(", ", names);
    }
}
