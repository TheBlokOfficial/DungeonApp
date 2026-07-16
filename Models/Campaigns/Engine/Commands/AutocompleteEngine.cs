using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Silnik parsowania i autouzupełniania komend — odpowiednik CommandDispatcher z Brigadiera.
/// Otrzymuje pełny tekst z TextBoxa, tokenizuje, sprawdza walidatory, nadaje kolory
/// i na końcu generuje ewentualne podpowiedzi.
/// </summary>
public class AutocompleteEngine
{
    private readonly List<CommandNode> _registeredRoots = new();

    public void RegisterCommand(CommandNode root, string ownerModuleId)
    {
        root.OwnerModuleId = ownerModuleId;
        if (_registeredRoots.All(r => r.Name != root.Name))
            _registeredRoots.Add(root);
    }

    public ParseResults Parse(string text, int caretIndex)
    {
        var inputUpToCaret = text.Length > caretIndex ? text[..caretIndex] : text;

        var tokens = SplitIntoTokens(inputUpToCaret);
        var parsedTokens = new List<ParsedToken>();
        
        if (tokens.Count == 0 || !text.StartsWith('/'))
        {
            if (!string.IsNullOrEmpty(text))
            {
                parsedTokens.Add(new ParsedToken(text, text, 0, text.Length, true, "White"));
            }
            return new ParseResults(parsedTokens, new SuggestionResult(Array.Empty<string>()));
        }

        var commandToken = tokens[0].Text;
        bool isInvalidCascading = false;
        
        string GetDisplayStr(int index, string txt) => index == tokens.Count - 1 ? txt : txt + " ";

        // --- Root Token ---
        var matchingRoot = _registeredRoots
            .FirstOrDefault(r => r.Name.Equals(commandToken, StringComparison.OrdinalIgnoreCase));

        if (matchingRoot == null)
        {
            if (commandToken.StartsWith('/'))
            {
                var slashStr = "/";
                var restStr = commandToken[1..];
                var slashDisplay = restStr.Length == 0 ? GetDisplayStr(0, slashStr) : slashStr;
                var restDisplay = restStr.Length > 0 ? GetDisplayStr(0, restStr) : "";
                
                parsedTokens.Add(new ParsedToken(slashStr, slashDisplay, tokens[0].StartIndex, 1, true, TokenColors.ValidLiteral));
                
                if (restStr.Length > 0)
                {
                    parsedTokens.Add(new ParsedToken(restStr, restDisplay, tokens[0].StartIndex + 1, restStr.Length, false, TokenColors.Invalid));
                }
            }
            else
            {
                parsedTokens.Add(new ParsedToken(commandToken, GetDisplayStr(0, commandToken), tokens[0].StartIndex, commandToken.Length, false, TokenColors.Invalid));
            }
            isInvalidCascading = true;
        }
        else
        {
            parsedTokens.Add(new ParsedToken(commandToken, GetDisplayStr(0, commandToken), tokens[0].StartIndex, commandToken.Length, true, TokenColors.ValidLiteral));
        }

        if (tokens.Count == 1)
        {
            // Tylko wpisany root, wyciągnij podpowiedzi
            var rootSuggestions = _registeredRoots
                .Where(r => r.Name.StartsWith(commandToken, StringComparison.OrdinalIgnoreCase))
                .SelectMany(r => r.Validator.GetSuggestions(commandToken, tokens[0].StartIndex).Suggestions)
                .ToList();
            return new ParseResults(parsedTokens, new SuggestionResult(rootSuggestions, tokens[0].StartIndex));
        }

        // Jeśli mamy więcej tokenów, a root nie pasuje, reszta też jest czerwona
        var currentNodes = matchingRoot?.Children ?? new List<CommandNode>();

        for (int i = 1; i < tokens.Count; i++)
        {
            var rawToken = tokens[i];
            var tokenStr = rawToken.Text;
            var displayStr = GetDisplayStr(i, tokenStr);

            if (isInvalidCascading)
            {
                parsedTokens.Add(new ParsedToken(tokenStr, displayStr, rawToken.StartIndex, tokenStr.Length, false, TokenColors.Invalid));
                continue;
            }

            if (string.IsNullOrEmpty(tokenStr))
            {
                // Spacja tworzy pusty token.
                parsedTokens.Add(new ParsedToken(tokenStr, displayStr, rawToken.StartIndex, tokenStr.Length, true, "Transparent"));
                continue;
            }

            CommandNode? matchedNode = null;
            ValidationResult? matchResult = null;

            foreach (var node in currentNodes)
            {
                var valRes = node.Validator.Parse(tokenStr);
                if (valRes.IsValid)
                {
                    matchedNode = node;
                    matchResult = valRes;
                    break;
                }
            }

            if (matchedNode == null)
            {
                // Nikt nie zaakceptował (np. wpisano złą literę dla NumberArgument)
                isInvalidCascading = true;
                parsedTokens.Add(new ParsedToken(tokenStr, displayStr, rawToken.StartIndex, tokenStr.Length, false, TokenColors.Invalid));

                // Ostatni token, nie jest skończony, ale daje podpowiedzi (np. "/time set m")
                if (i == tokens.Count - 1)
                {
                    var allSuggestions = new List<string>();
                    int overrideStart = rawToken.StartIndex;

                    foreach (var n in currentNodes)
                    {
                        var sugg = n.Validator.GetSuggestions(tokenStr, rawToken.StartIndex);
                        allSuggestions.AddRange(sugg.Suggestions);
                        if (sugg.TokenStartOverride.HasValue) overrideStart = sugg.TokenStartOverride.Value;
                    }

                    if (allSuggestions.Any())
                    {
                        return new ParseResults(parsedTokens, new SuggestionResult(allSuggestions, overrideStart));
                    }
                }
            }
            else
            {
                // Dopasowano token
                parsedTokens.Add(new ParsedToken(tokenStr, displayStr, rawToken.StartIndex, tokenStr.Length, true, matchResult!.Color));
                
                if (matchedNode.IsGreedy)
                {
                    // Zachłanny string pochłania całą resztę tekstu, włącznie ze spacjami!
                    int greedyStartIndex = rawToken.StartIndex;
                    string greedyText = inputUpToCaret.Substring(greedyStartIndex);
                    
                    // Podmieniamy dodany wyżej ParsedToken na taki, który obejmuje całość (aż do Careta)
                    parsedTokens.RemoveAt(parsedTokens.Count - 1);
                    parsedTokens.Add(new ParsedToken(greedyText, greedyText, greedyStartIndex, greedyText.Length, true, matchResult!.Color));
                    
                    // Jeżeli to był ostatni token i nie ma w nim spacji (czyli i == tokens.Count - 1), 
                    // i tak chcemy zwrócić pustą listę podpowiedzi.
                    return new ParseResults(parsedTokens, new SuggestionResult(Array.Empty<string>()));
                }
                
                if (i == tokens.Count - 1)
                {
                    // To jest ostatni token (i nie greedy). Zwróćmy dla niego sugestie
                    var suggestions = matchedNode.Validator.GetSuggestions(tokenStr, rawToken.StartIndex);
                    int finalStart = suggestions.TokenStartOverride ?? rawToken.StartIndex;
                    return new ParseResults(parsedTokens, new SuggestionResult(suggestions.Suggestions, finalStart));
                }

                currentNodes = matchedNode.Children;
            }
        }

        // Jeżeli pętla się skończyła po spacji: chcemy podpowiedzi dla dzieci
        if (!isInvalidCascading && tokens[^1].Text == string.Empty)
        {
            var rawToken = tokens[^1];
            var allSuggestions = new List<string>();
            int overrideStart = rawToken.StartIndex;
            
            foreach(var n in currentNodes)
            {
                var sugg = n.Validator.GetSuggestions("", rawToken.StartIndex);
                allSuggestions.AddRange(sugg.Suggestions);
                if (sugg.TokenStartOverride.HasValue) overrideStart = sugg.TokenStartOverride.Value;
            }

            return new ParseResults(parsedTokens, new SuggestionResult(allSuggestions, overrideStart));
        }

        return new ParseResults(parsedTokens, new SuggestionResult(Array.Empty<string>()));
    }

    public CommandResult Execute(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/')) return CommandResult.Silent();

        var tokens = SplitIntoTokens(text);
        if (tokens.Count == 0) return CommandResult.Silent();

        var commandToken = tokens[0].Text;
        var matchingRoot = _registeredRoots.FirstOrDefault(r => r.Name.Equals(commandToken, StringComparison.OrdinalIgnoreCase));
        if (matchingRoot == null) return CommandResult.Error($"Nieznana komenda: {commandToken}");

        var context = new CommandContext(text);
        var currentNodes = matchingRoot.Children;
        CommandNode lastMatchedNode = matchingRoot;

        for (int i = 1; i < tokens.Count; i++)
        {
            var rawToken = tokens[i];
            var tokenStr = rawToken.Text;

            if (string.IsNullOrEmpty(tokenStr) && i == tokens.Count - 1)
            {
                break; // Ignoruj pusty token (spację) na samym końcu przy egzekucji
            }

            CommandNode? matchedNode = null;
            ValidationResult? matchResult = null;

            foreach (var node in currentNodes)
            {
                var valRes = node.Validator.Parse(tokenStr);
                if (valRes.IsValid)
                {
                    matchedNode = node;
                    matchResult = valRes;
                    break;
                }
            }

            if (matchedNode == null)
            {
                return CommandResult.Error($"Zła składnia w miejscu: {tokenStr}");
            }

            if (matchResult!.ParsedValue != null)
            {
                context.SetArgument(matchedNode.Name, matchResult.ParsedValue);
            }

            lastMatchedNode = matchedNode;
            
            if (matchedNode.IsGreedy)
            {
                int greedyStartIndex = rawToken.StartIndex;
                string greedyText = text.Substring(greedyStartIndex);
                context.SetArgument(matchedNode.Name, greedyText);
                break; // Greedy pożera wszystko do końca zdania
            }
            
            currentNodes = matchedNode.Children;
        }

        if (lastMatchedNode.ExecutionAction != null)
        {
            var result = lastMatchedNode.ExecutionAction.Invoke(context);
            if (result.SenderId == null && matchingRoot.OwnerModuleId != null)
            {
                result.SenderId = matchingRoot.OwnerModuleId;
            }
            return result;
        }

        return CommandResult.Error("Zła składnia komendy: brakuje argumentów.");
    }

    private List<(string Text, int StartIndex)> SplitIntoTokens(string input)
    {
        var result = new List<(string Text, int StartIndex)>();
        int startIndex = 0;
        var parts = input.Split(' ');
        
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            result.Add((p, startIndex));
            startIndex += p.Length + 1;
        }
        return result;
    }
}
