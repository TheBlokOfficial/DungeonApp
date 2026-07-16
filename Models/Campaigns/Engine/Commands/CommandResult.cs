using System;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Rezultat wykonania komendy wymuszony przez interfejs ICommand / Executes().
/// Gwarantuje, że programista obsłuży informację zwrotną dla użytkownika,
/// albo intencjonalnie ją zignoruje za pomocą <see cref="Silent"/>.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Czy odpowiedź ma zostać zignorowana przez silnik powiadomień.
    /// </summary>
    public bool IsSilent { get; }

    /// <summary>
    /// Treść informacji zwrotnej do wyświetlenia.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Poziom wiadomości (zgodny z poziomami w NotificationEvent: "Info", "Warning", "Error").
    /// </summary>
    public string Level { get; }

    /// <summary>
    /// Identyfikator modułu wywołującego (wstrzykiwany przez silnik).
    /// </summary>
    public string? SenderId { get; internal set; }

    private CommandResult(bool isSilent, string message, string level)
    {
        IsSilent = isSilent;
        Message = message;
        Level = level;
    }

    /// <summary>
    /// Zwraca pomyślny rezultat komendy z powiadomieniem (Info).
    /// </summary>
    public static CommandResult Success(string message) => new(false, message, "Info");

    /// <summary>
    /// Zwraca błąd lub ostrzeżenie z powiadomieniem.
    /// </summary>
    public static CommandResult Error(string message, bool isWarning = false) 
        => new(false, message, isWarning ? "Warning" : "Error");

    /// <summary>
    /// Zwraca informację dla użytkownika (Info).
    /// </summary>
    public static CommandResult Info(string message) => new(false, message, "Info");

    /// <summary>
    /// Niema komenda. Zwraca wynik pomyślny bez generowania komunikatu zwrotnego.
    /// </summary>
    public static CommandResult Silent() => new(true, string.Empty, "Info");
}
