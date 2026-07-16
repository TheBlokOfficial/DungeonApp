using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

public class EnumArgumentType : IArgumentType
{
    private readonly IReadOnlyList<string> _values;

    public EnumArgumentType(IReadOnlyList<string> values)
    {
        _values = values;
    }

    public ValidationResult Parse(string token)
    {
        // Sprawdzamy czy wpisany token idealnie pasuje do którejś z dozwolonych opcji
        if (_values.Any(v => v.Equals(token, System.StringComparison.OrdinalIgnoreCase)))
        {
            return new ValidationResult(true, TokenColors.ValidLiteral, token);
        }
        return new ValidationResult(false, TokenColors.Invalid);
    }

    public SuggestionResult GetSuggestions(string token, int currentTokenStart)
    {
        var filtered = _values.Where(v => v.StartsWith(token, System.StringComparison.OrdinalIgnoreCase));
        return new SuggestionResult(filtered);
    }
}
