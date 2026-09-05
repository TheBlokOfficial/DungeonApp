using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DungeonApp.Desktop.Themes;

namespace MockupRenderer;

/// <summary>
/// Parsowanie argumentów wiersza poleceń narzędzia. Dwa argumenty pozycyjne
/// (plik wejściowy .axaml i wyjściowy .png), reszta to opcjonalne flagi.
/// Złe argumenty kończą się komunikatem i niepowodzeniem, nigdy wyjątkiem.
/// </summary>
internal sealed class CliOptions
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public double Width { get; init; } = 1280;
    public double Height { get; init; } = 800;
    public UiScaleProfile ScaleProfile { get; init; } = UiScaleProfile.Medium;
    public string? DataContextPath { get; init; }

    public const string Usage =
        "Użycie: MockupRenderer <wejście.axaml> <wyjście.png> " +
        "[--width=N] [--height=N] [--scale=Small|Medium|Large] [--data=kontekst.json]";

    public static bool TryParse(string[] args, out CliOptions options, out string error)
    {
        options = null!;
        error = string.Empty;

        var positional = new List<string>();
        double width = 1280;
        double height = 800;
        var scale = UiScaleProfile.Medium;
        string? dataPath = null;

        foreach (var arg in args)
        {
            if (TryReadFlag(arg, "--width=", out var widthValue))
            {
                if (!double.TryParse(widthValue, NumberStyles.Float, CultureInfo.InvariantCulture, out width) || width <= 0)
                {
                    error = $"Niepoprawna szerokość: '{widthValue}'.";
                    return false;
                }
            }
            else if (TryReadFlag(arg, "--height=", out var heightValue))
            {
                if (!double.TryParse(heightValue, NumberStyles.Float, CultureInfo.InvariantCulture, out height) || height <= 0)
                {
                    error = $"Niepoprawna wysokość: '{heightValue}'.";
                    return false;
                }
            }
            else if (TryReadFlag(arg, "--scale=", out var scaleValue))
            {
                if (!Enum.TryParse(scaleValue, ignoreCase: true, out scale))
                {
                    error = $"Nieznany profil skalowania: '{scaleValue}' (dozwolone: Small, Medium, Large).";
                    return false;
                }
            }
            else if (TryReadFlag(arg, "--data=", out var dataValue))
            {
                dataPath = dataValue;
            }
            else if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Nieznana flaga: '{arg}'.";
                return false;
            }
            else
            {
                positional.Add(arg);
            }
        }

        if (positional.Count != 2)
        {
            error = $"Wymagane są dokładnie dwa argumenty pozycyjne, otrzymano {positional.Count}.";
            return false;
        }

        if (dataPath is not null && !File.Exists(dataPath))
        {
            error = $"Plik kontekstu danych nie istnieje: {dataPath}";
            return false;
        }

        options = new CliOptions
        {
            InputPath = positional[0],
            OutputPath = positional[1],
            Width = width,
            Height = height,
            ScaleProfile = scale,
            DataContextPath = dataPath,
        };
        return true;
    }

    private static bool TryReadFlag(string arg, string prefix, out string value)
    {
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = arg[prefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }
}
