using System.Collections.Generic;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

public class LiteralArgumentType : IArgumentType
{
    private readonly string _literal;

    public LiteralArgumentType(string literal)
    {
        _literal = literal;
    }

    public ValidationResult Parse(string token)
    {
        if (token.Equals(_literal, System.StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(true, TokenColors.ValidLiteral, token);
        }
        return new ValidationResult(false, TokenColors.Invalid);
    }

    public SuggestionResult GetSuggestions(string token, int currentTokenStart)
    {
        if (_literal.StartsWith(token, System.StringComparison.OrdinalIgnoreCase))
        {
            return new SuggestionResult(new[] { _literal });
        }
        return new SuggestionResult(System.Array.Empty<string>());
    }
}
