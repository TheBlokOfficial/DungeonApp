using System.Collections.Generic;
using DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Informacje o sparsowanym pojedynczym słowie z wejścia konsoli.
/// Zapewnia dane niezbędne do pokolorowania elementu w UI.
/// </summary>
public record ParsedToken(string Text, string DisplayText, int StartIndex, int Length, bool IsValid, string Color);

/// <summary>
/// Pełny wynik pracy silnika autouzupełniania:
/// Pocięte i zwalidowane tokeny (do rysowania UI) oraz podpowiedzi (do ListBoxa).
/// </summary>
public record ParseResults(IReadOnlyList<ParsedToken> Tokens, SuggestionResult Suggestions);
