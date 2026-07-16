using System.Collections.Generic;
using DungeonApp.Models.Campaigns.Engine.Commands.Arguments;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Fluent Builder API do budowania drzewa komend — odpowiednik LiteralArgumentBuilder z Brigadiera.
/// </summary>
public class CommandTree
{
    /// <summary>Korzeń budowanego poddrzewa.</summary>
    public CommandNode Root { get; }

    private CommandTree(CommandNode root)
    {
        Root = root;
    }

    // ─── Factory Methods ──────────

    /// <summary>Tworzy węzeł Literal — stałe słowo kluczowe (prefix komendy lub sub-komenda).</summary>
    public static CommandTree Literal(string name)
        => new(new CommandNode(name, new LiteralArgumentType(name)));

    /// <summary>Tworzy węzeł EnumArgument — argument z listą dostępnych wartości do wyboru.</summary>
    public static CommandTree Enum(string name, params string[] values)
        => new(new CommandNode(name, new EnumArgumentType(values)));

    /// <summary>Tworzy węzeł NumberArgument — użytkownik wpisuje liczbę.</summary>
    public static CommandTree Number(string name, int min = int.MinValue, int max = int.MaxValue)
        => new(new CommandNode(name, new IntegerArgumentType(min, max, name)));

    /// <summary>Tworzy węzeł TimeArgument — liczba z opcjonalnym sufiksem (s, m, h, d).</summary>
    public static CommandTree Time(string name)
        => new(new CommandNode(name, new TimeArgumentType(name)));

    /// <summary>Tworzy węzeł StringArgument — użytkownik wpisuje dowolny tekst.</summary>
    public static CommandTree String(string name, bool isGreedy = false)
    {
        var node = new CommandNode(name, new GreedyStringArgumentType(name));
        node.IsGreedy = isGreedy;
        return new CommandTree(node);
    }

    // ─── Builder Methods ─────────────────────────────────────────────────────

    /// <summary>Dodaje dziecko do bieżącego węzła.</summary>
    public CommandTree Then(CommandTree child)
    {
        Root.Children.Add(child.Root);
        return this;
    }

    /// <summary>Oznacza węzeł jako wykonywalny i przypisuje mu akcję do wykonania.</summary>
    public CommandTree Executes(System.Func<CommandContext, CommandResult> action)
    {
        Root.ExecutionAction = action;
        return this;
    }
}
