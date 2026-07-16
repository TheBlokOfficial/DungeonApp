using System.Collections.Generic;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

/// <summary>
/// Kolory używane do podświetlania składni tokenów w konsoli.
/// </summary>
public static class TokenColors
{
    public const string ValidLiteral = "LightGray";
    public const string ValidNumber = "Cyan";
    public const string Invalid = "#FF5555"; // Czerwony Minecraftowy
}

public record ValidationResult(bool IsValid, string Color, object? ParsedValue = null);

public record SuggestionResult(IEnumerable<string> Suggestions, int? TokenStartOverride = null);

/// <summary>
/// Definiuje sposób walidacji, kolorowania i autouzupełniania dla konkretnego typu argumentu.
/// </summary>
public interface IArgumentType
{
    /// <summary>
    /// Parsuje pojedynczy token tekstowy (słowo).
    /// </summary>
    ValidationResult Parse(string token);

    /// <summary>
    /// Zwraca podpowiedzi dla wpisanego częściowo tokenu.
    /// Możliwość nadpisania TokenStart (np. dla jednostek, by podpowiadać po liczbie).
    /// </summary>
    SuggestionResult GetSuggestions(string token, int currentTokenStart);
}
