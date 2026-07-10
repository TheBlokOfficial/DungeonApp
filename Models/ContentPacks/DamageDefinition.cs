using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DungeonApp.Models.ContentPacks;

/// <summary>
/// Strukturyzowana reprezentacja obrażeń w systemie.
/// Umożliwia łatwe używanie ciągów tekstowych typu "2d6+2" w JSONie,
/// jednocześnie udostępniając czyste wartości matematyczne dla silnika filtrującego w C#.
/// </summary>
public class DamageDefinition
{
    /// <summary>
    /// Przyjazny zapis dla twórców paczek (np. "1d8", "2d6+2").
    /// </summary>
    public string Roll { get; set; } = string.Empty;

    /// <summary>
    /// Klucz typu obrażeń z systemu tłumaczeń (np. "core_propval_slashing").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    // ----- Właściwości Ukryte (Tylko do odczytu przez silnik i filtry) -----

    [JsonIgnore] public int DiceCount { get; private set; }
    [JsonIgnore] public int DiceSides { get; private set; }
    [JsonIgnore] public int Bonus { get; private set; }
    
    [JsonIgnore] 
    public double AverageDamage => (DiceCount * ((DiceSides / 2.0) + 0.5)) + Bonus;

    [JsonIgnore]
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Metoda parsująca ciąg z właściwości Roll. Powinna być wywołana zaraz po wczytaniu z JSONa.
    /// Weryfikuje format i ustala pola liczbowe.
    /// Ustawia flagę IsValid = false, jeśli format jest wadliwy.
    /// </summary>
    public void ParseRoll()
    {
        if (string.IsNullOrWhiteSpace(Roll))
        {
            IsValid = false;
            return;
        }

        try
        {
            // Oczekiwany format: [liczba]d[liczba] np. 1d8, 2d6, 10d100
            // Opcjonalny bonus: +[liczba] lub -[liczba] np. 2d4+2, 1d12-1
            // Ignoruje spacje np. " 1 d 8 + 2 " -> "1d8+2"
            var cleanRoll = Roll.Replace(" ", "").ToLowerInvariant();

            var match = Regex.Match(cleanRoll, @"^(\d*)d(\d+)([\+\-]\d+)?$");
            
            if (!match.Success)
            {
                IsValid = false;
                return;
            }

            // DiceCount (Domyślnie 1, jeśli wpisano np. "d8" zamiast "1d8")
            if (string.IsNullOrEmpty(match.Groups[1].Value))
            {
                DiceCount = 1;
            }
            else
            {
                DiceCount = int.Parse(match.Groups[1].Value);
            }

            // DiceSides
            DiceSides = int.Parse(match.Groups[2].Value);

            // Bonus
            if (match.Groups[3].Success && !string.IsNullOrEmpty(match.Groups[3].Value))
            {
                Bonus = int.Parse(match.Groups[3].Value);
            }
            else
            {
                Bonus = 0;
            }

            IsValid = true;
        }
        catch
        {
            IsValid = false;
        }
    }
}
