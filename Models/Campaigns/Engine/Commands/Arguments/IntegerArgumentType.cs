using System.Collections.Generic;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

public class IntegerArgumentType : IArgumentType
{
    private readonly int _min;
    private readonly int _max;
    private readonly string _hintName;

    public IntegerArgumentType(int min = int.MinValue, int max = int.MaxValue, string hintName = "liczba")
    {
        _min = min;
        _max = max;
        _hintName = hintName;
    }

    public ValidationResult Parse(string token)
    {
        if (int.TryParse(token, out int result))
        {
            if (result >= _min && result <= _max)
            {
                return new ValidationResult(true, TokenColors.ValidNumber, result);
            }
        }
        return new ValidationResult(false, TokenColors.Invalid);
    }

    public SuggestionResult GetSuggestions(string token, int currentTokenStart)
    {
        return new SuggestionResult(new[] { $"{CommandNode.HintPrefix}{_hintName}>" });
    }
}
