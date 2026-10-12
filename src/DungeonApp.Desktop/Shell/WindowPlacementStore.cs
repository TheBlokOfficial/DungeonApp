using System;
using System.IO;
using System.Text.Json;
using DungeonApp.Desktop.Diagnostics;

namespace DungeonApp.Desktop.Shell;

/// <summary>
/// What the main window remembers between runs: maximized or not, and the size of the window in its
/// normal state (so restoring a maximized window returns to the remembered size). The position is
/// deliberately not kept - a monitor that is gone would leave the window off-screen.
/// </summary>
public sealed record WindowPlacement(bool IsMaximized, double Width, double Height);

/// <summary>
/// One small JSON file in the application data folder - a setting of the application, not of any
/// campaign. Reading is lenient: a missing or damaged file means defaults (the damage is logged),
/// because a lost window size must never keep the application from starting. Writing failures are
/// logged and swallowed for the same reason: this runs while the window closes.
/// </summary>
public sealed class WindowPlacementStore(string path)
{
    /// <summary>Bounds that keep a hand-edited or damaged value from producing an unusable window.
    /// The window's own minimum size (theme tokens) still applies on top.</summary>
    public const double MinimumSize = 200;

    public const double MaximumSize = 16384;

    public WindowPlacement? Load()
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var placement = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(path));
            if (placement is null || !double.IsFinite(placement.Width) || !double.IsFinite(placement.Height))
            {
                throw new JsonException("Brak poprawnych wymiarów okna.");
            }

            return placement with
            {
                Width = Math.Clamp(placement.Width, MinimumSize, MaximumSize),
                Height = Math.Clamp(placement.Height, MinimumSize, MaximumSize),
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            AppLog.Error($"Nie udało się wczytać położenia okna z {path}; użyte wartości domyślne.", ex);
            return null;
        }
    }

    public void Save(WindowPlacement placement)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(placement));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error($"Nie udało się zapisać położenia okna do {path}.", ex);
        }
    }
}
