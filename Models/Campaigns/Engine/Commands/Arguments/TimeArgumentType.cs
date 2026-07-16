using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

public class TimeArgumentType : IArgumentType
{
    private static readonly string[] _suffixes = { "s", "m", "h", "d" };
    private readonly string _hintName;

    public TimeArgumentType(string hintName = "czas")
    {
        _hintName = hintName;
    }

    public ValidationResult Parse(string token)
    {
        if (string.IsNullOrEmpty(token))
            return new ValidationResult(false, TokenColors.Invalid);

        // Próba odcięcia ewentualnego poprawnego sufiksu na końcu
        string numberPart = token;
        bool hasSuffix = false;

        foreach (var suffix in _suffixes)
        {
            if (token.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            {
                numberPart = token.Substring(0, token.Length - suffix.Length);
                hasSuffix = true;
                break;
            }
        }

        if (int.TryParse(numberPart, out int result) && result >= 0)
        {
            int multiplier = 1;
            if (hasSuffix)
            {
                var usedSuffix = token.Substring(numberPart.Length).ToLowerInvariant();
                multiplier = usedSuffix switch
                {
                    "s" => 1,
                    "m" => 60,
                    "h" => 3600,
                    "d" => 86400,
                    _ => 1
                };
            }
            int totalSeconds = result * multiplier;
            return new ValidationResult(true, TokenColors.ValidNumber, totalSeconds);
        }

        return new ValidationResult(false, TokenColors.Invalid);
    }

    public SuggestionResult GetSuggestions(string token, int currentTokenStart)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new SuggestionResult(new[] { $"{CommandNode.HintPrefix}{_hintName}>" });
        }

        // Zobaczmy, czy wpisano już samą poprawną liczbę bez sufiksu
        if (int.TryParse(token, out int result) && result >= 0)
        {
            // Magia Minecrafta: Jeżeli wpisano "5", to przesuwamy kursor za cyfrę 5,
            // i proponujemy same jednostki!
            int shiftedTokenStart = currentTokenStart + token.Length;
            return new SuggestionResult(_suffixes, shiftedTokenStart);
        }
        else
        {
            // Sprawdźmy, czy użytkownik jest w trakcie wpisywania sufiksu
            string numberPart = string.Empty;
            string currentSuffix = string.Empty;

            // Szukamy punktu, gdzie kończą się cyfry
            for (int i = 0; i < token.Length; i++)
            {
                if (!char.IsDigit(token[i]))
                {
                    numberPart = token.Substring(0, i);
                    currentSuffix = token.Substring(i);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(numberPart) && int.TryParse(numberPart, out _) && !string.IsNullOrEmpty(currentSuffix))
            {
                // Znaleziono liczbę i początek sufiksu
                var validSuffixes = _suffixes.Where(s => s.StartsWith(currentSuffix, System.StringComparison.OrdinalIgnoreCase));
                int shiftedTokenStart = currentTokenStart + numberPart.Length;
                
                if (validSuffixes.Any())
                {
                    return new SuggestionResult(validSuffixes, shiftedTokenStart);
                }
            }
        }

        return new SuggestionResult(System.Array.Empty<string>());
    }
}
