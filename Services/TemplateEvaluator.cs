using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DungeonApp.Services;

/// <summary>
/// Silnik ewaluacji dynamicznych szablonów tekstowych.
/// Służy do wyciągania i wstrzykiwania wartości z obiektów na podstawie ścieżek
/// zapisanych w nawiasach klamrowych, np. "{Weapon.DamageRoll}".
/// </summary>
public static class TemplateEvaluator
{
    private static readonly Regex TokenRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Ocenia i formatuje zadany ciąg na podstawie właściwości dostarczonego obiektu.
    /// Jeżeli jakikolwiek token odnosi się do nieistniejącej lub pustej (null) wartości,
    /// CAŁY proces zwraca null, ignorując formatowanie (np. aby ukryć puste odznaki).
    /// </summary>
    /// <param name="format">Wzorzec, np. "{Weapon.DamageRoll} kłutych"</param>
    /// <param name="context">Obiekt kontekstu, np. ItemComponents</param>
    /// <returns>Gotowy, sformatowany string, lub null jeśli ewaluacja się nie powiodła.</returns>
    public static string? Evaluate(string format, object? context)
    {
        if (string.IsNullOrWhiteSpace(format))
            return null;

        if (context == null)
            return null; // Brak kontekstu do ewaluacji
            
        var matches = TokenRegex.Matches(format);
        if (matches.Count == 0)
            return format; // To po prostu zwykły statyczny tekst (choć rzadko używany w tym systemie)

        var result = format;

        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value;
            var val = GetPropertyValue(context, path);

            // Zabezpieczenie: jeśli w obiekcie nie ma danego komponentu (np. Armor = null)
            // lub wartość jest pusta, to odrzucamy CAŁĄ odznakę, żeby nie śmiecić UI.
            if (val == null || string.IsNullOrWhiteSpace(val.ToString()))
                return null;

            result = result.Replace(match.Value, val.ToString());
        }

        return result;
    }

    /// <summary>
    /// Odszukuje wartość właściwości przy pomocy Refleksji, wspierając kropki (np. "Weapon.DamageRoll").
    /// </summary>
    private static object? GetPropertyValue(object? obj, string path)
    {
        if (obj == null) return null;

        var parts = path.Split('.');
        foreach (var part in parts)
        {
            if (obj == null) return null;

            var propInfo = obj.GetType().GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null) return null; // Brak takiej właściwości

            obj = propInfo.GetValue(obj);
        }

        return obj;
    }
}
