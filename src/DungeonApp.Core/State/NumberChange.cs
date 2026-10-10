using System;
using System.Globalization;

namespace DungeonApp.Core.State;

/// <summary>What a change field does to a tracked number: take away, add, or set.</summary>
public enum NumberChangeKind
{
    Subtract,
    Add,
    Set,
}

/// <summary>
/// One entry in a tracked value's change field: "-12", "+5" or "=30". A bare number is refused,
/// because "12" typed as damage would set the value to 12 instead of taking 12 away. Only the field
/// belongs to the frame; what the change does to the value (floors, ceilings, which pool goes first)
/// is the system's arithmetic.
/// </summary>
public readonly record struct NumberChange(NumberChangeKind Kind, int Amount)
{
    // The typographic minus is accepted too: it is what the preview shows, so a GM who copies it back
    // gets the same change.
    private const char TypographicMinus = '−';

    /// <summary>
    /// Reads a change from the field's text. Surrounding spaces and a space after the sign are allowed.
    /// "-" and "+" take a non-negative amount; "=" takes any whole number, so a value can be set below
    /// zero - a state against the rules shows instead of being blocked.
    /// </summary>
    public static bool TryParse(string? text, out NumberChange change)
    {
        change = default;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        NumberChangeKind kind;
        switch (trimmed[0])
        {
            case '-' or TypographicMinus:
                kind = NumberChangeKind.Subtract;
                break;
            case '+':
                kind = NumberChangeKind.Add;
                break;
            case '=':
                kind = NumberChangeKind.Set;
                break;
            default:
                return false;
        }

        var rest = trimmed[1..].TrimStart();
        var negative = false;
        if (kind == NumberChangeKind.Set && rest.Length > 0 && rest[0] is '-' or TypographicMinus)
        {
            negative = true;
            rest = rest[1..].TrimStart();
        }

        if (!int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        change = new NumberChange(kind, negative ? -amount : amount);
        return true;
    }

    /// <summary>The change as the preview shows it: "−5", "+3", "=10", with the typographic minus.</summary>
    public override string ToString()
    {
        var amount = Amount.ToString(CultureInfo.InvariantCulture).Replace('-', TypographicMinus);
        return Kind switch
        {
            NumberChangeKind.Subtract => $"{TypographicMinus}{amount}",
            NumberChangeKind.Add => $"+{amount}",
            NumberChangeKind.Set => $"={amount}",
            _ => throw new InvalidOperationException($"Unknown change kind {Kind}."),
        };
    }
}
