using System;
using System.Collections.Generic;

namespace DungeonApp.Models.Campaigns.Engine.Commands;

/// <summary>
/// Kontekst wykonania komendy, zawierający wszystkie pomyślnie wyekstrahowane
/// i zrzutowane na właściwe typy parametry dla danej komendy.
/// </summary>
public class CommandContext
{
    private readonly Dictionary<string, object> _arguments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Oryginalny, surowy tekst wejściowy.
    /// </summary>
    public string Input { get; }

    public CommandContext(string input)
    {
        Input = input;
    }

    /// <summary>
    /// Zapisuje sparsowaną wartość do słownika.
    /// </summary>
    public void SetArgument(string name, object value)
    {
        _arguments[name] = value;
    }

    /// <summary>
    /// Pobiera wyekstrahowany i rzutowany argument na podstawie jego nazwy.
    /// </summary>
    /// <typeparam name="T">Oczekiwany typ (np. int, string).</typeparam>
    /// <param name="name">Nazwa argumentu (zadeklarowana w definicji komendy).</param>
    /// <returns>Zrzutowana wartość argumentu.</returns>
    /// <exception cref="ArgumentException">Zgłaszany, jeśli argument nie istnieje.</exception>
    /// <exception cref="InvalidCastException">Zgłaszany, jeśli typ się nie zgadza.</exception>
    public T GetArgument<T>(string name)
    {
        if (_arguments.TryGetValue(name, out var obj))
        {
            if (obj is T castedValue)
            {
                return castedValue;
            }
            throw new InvalidCastException($"Argument '{name}' is of type {obj.GetType().Name}, not {typeof(T).Name}.");
        }
        throw new ArgumentException($"Argument '{name}' is not present in the CommandContext.");
    }
}
