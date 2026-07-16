using System.Collections.Generic;
using DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Jeden węzeł drzewa komend — odpowiednik CommandNode w Brigadierze.
/// </summary>
public class CommandNode
{
    /// <summary>Prefiks używany do oznaczania nieklikalnych argumentów w interfejsie użytkownika.</summary>
    public const string HintPrefix = "<";

    /// <summary>Nazwa węzła — używana głównie identyfikacyjnie oraz w podpowiedziach.</summary>
    public string Name { get; }

    /// <summary>Walidator odpowiedzialny za analizę, kolorowanie i autouzupełnianie tego argumentu.</summary>
    public IArgumentType Validator { get; }

    /// <summary>Dzieci tego węzła — kolejny poziom komend lub argumentów.</summary>
    public List<CommandNode> Children { get; } = new();

    /// <summary>Czy węzeł jest "zachłanny" (Greedy).</summary>
    public bool IsGreedy { get; set; }

    /// <summary>Akcja do wykonania po dotarciu do tego węzła.</summary>
    public System.Func<CommandContext, CommandResult>? ExecutionAction { get; set; }

    /// <summary>Moduł, do którego należy ta komenda (ustawiane automatycznie przez silnik).</summary>
    public string? OwnerModuleId { get; set; }

    public CommandNode(string name, IArgumentType validator)
    {
        Name = name;
        Validator = validator;
    }
}
