using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DungeonApp.Models;

namespace DungeonApp.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    event Action? SettingsChanged;
}

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppPaths.AppDataPath, "settings.yml");
        
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            string yaml = File.ReadAllText(_settingsFilePath);
            var settings = _deserializer.Deserialize<AppSettings>(yaml);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd wczytywania ustawień: {ex.Message}");
            return new AppSettings();
        }
    }

    public event Action? SettingsChanged;

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            string yaml = _serializer.Serialize(settings);
            File.WriteAllText(_settingsFilePath, yaml);
            SettingsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd zapisywania ustawień: {ex.Message}");
        }
    }
}
