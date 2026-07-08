using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DungeonApp.Services;

public class FileStorageService : IStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public List<T> LoadAll<T>(string directoryPath) where T : class
    {
        var items = new List<T>();
        if (!Directory.Exists(directoryPath)) return items;

        foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
        {
            var item = Load<T>(file);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public T? Load<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd wczytywania pliku '{filePath}': {ex.Message}");
            return null;
        }
    }

    public void Save<T>(string filePath, T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd zapisywania pliku '{filePath}': {ex.Message}");
        }
    }

    public void Delete(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd usuwania pliku '{filePath}': {ex.Message}");
        }
    }

    public void DeleteDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath)) return;

        try
        {
            Directory.Delete(directoryPath, recursive: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd usuwania katalogu '{directoryPath}': {ex.Message}");
        }
    }
}
