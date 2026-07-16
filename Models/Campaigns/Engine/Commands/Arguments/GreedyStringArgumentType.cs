using System.Collections.Generic;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

public class GreedyStringArgumentType : IArgumentType
{
    private readonly string _hintName;

    public GreedyStringArgumentType(string hintName = "tekst")
    {
        _hintName = hintName;
    }

    public ValidationResult Parse(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new ValidationResult(false, TokenColors.Invalid);
        return new ValidationResult(true, TokenColors.ValidLiteral, token);
    }

    public SuggestionResult GetSuggestions(string token, int currentTokenStart)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new SuggestionResult(new[] { $"{CommandNode.HintPrefix}{_hintName}>" });
        }
        
        // Brak innych sugestii dla tekstu wolnego (greedystring) w trakcie pisania
        return new SuggestionResult(System.Array.Empty<string>());
    }
}
