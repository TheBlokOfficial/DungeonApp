using System;
using System.IO;
using System.Text.Json;
using DungeonApp.Desktop.Themes;

namespace DungeonApp.Desktop.Settings;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> as a single JSON file, using a safe-write
/// (temp file + atomic move) pattern.
/// </summary>
public sealed class AppSettingsStore(string directoryPath)
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Loads persisted settings. A missing or corrupt file is not an error: it falls back to
    /// <see cref="AppSettings.Default"/> rather than blocking startup.
    /// </summary>
    public AppSettings Load()
    {
        var path = GetPath();

        if (!File.Exists(path))
        {
            return AppSettings.Default;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<SettingsDocument>(stream, _serializerOptions);
            if (document is null)
            {
                return AppSettings.Default;
            }

            return new AppSettings(document.ScaleProfile);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(directoryPath);

        var document = new SettingsDocument(settings.ScaleProfile);
        var destinationPath = GetPath();
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, document, _serializerOptions);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string GetPath() => Path.Combine(directoryPath, "settings.json");

    private sealed record SettingsDocument(UiScaleProfile ScaleProfile);
}
