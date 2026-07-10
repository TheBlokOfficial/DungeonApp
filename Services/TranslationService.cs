using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Platform;
namespace DungeonApp.Services;

public class TranslationService : ITranslationService
{
    private Dictionary<string, string> _translations = new();
    private string _currentLanguage = "en";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                ChangeLanguage(value);
            }
        }
    }

    // Pozwala na używanie serwisu jako słownika: translator["key"]
    public string this[string key] => Translate(key);

    public TranslationService(ISettingsService settingsService)
    {
        var settings = settingsService.LoadSettings();
        ChangeLanguage(settings.Language ?? "en");
    }

    public void ChangeLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        LoadLanguageFile(languageCode);
        
        // Zgłoszenie zmiany dla indexera Item[] odświeży wszystkie bindingi Avalonia Markup używające tego serwisu!
        OnPropertyChanged(string.Empty); // Zmiana wszystkiego
        OnPropertyChanged("Item"); // Avalonia-specific binding update for indexer
        OnPropertyChanged("Item[]"); // Standardowy C# indexer property name
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    public string Translate(string? key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return _translations.TryGetValue(key, out var value) ? value : key;
    }

    private void LoadLanguageFile(string langCode)
    {
        _translations = new Dictionary<string, string>();

        // 1. Wczytywanie wbudowanych tłumaczeń z zasobów aplikacji
        try
        {
            var uri = new Uri($"avares://DungeonApp/Assets/Lang/{langCode}.json");
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                foreach (var kvp in dict) _translations[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranslationService] Błąd ładowania pliku języka wbudowanego {langCode}: {ex.Message}");
        }

        // 2. Mergowanie tłumaczeń z zainstalowanych paczek (Pack-level I18n)
        try
        {
            var packDirectoriesToScan = new List<string>
            {
                AppPaths.BuiltInPacksPath,
                Path.Combine(AppPaths.UserDataPath, "packs")
            };

            foreach (var packsDir in packDirectoriesToScan)
            {
                if (Directory.Exists(packsDir))
                {
                    // Scan unzipped folders
                    foreach (var packDir in Directory.GetDirectories(packsDir))
                    {
                        var packLangPath = Path.Combine(packDir, "lang", $"{langCode}.json");
                        if (File.Exists(packLangPath))
                        {
                            try
                            {
                                var json = File.ReadAllText(packLangPath);
                                MergeTranslations(json);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[TranslationService] Błąd parsowania {packLangPath}: {ex.Message}");
                            }
                        }
                    }

                    // Scan zipped packs
                    foreach (var zipFile in Directory.GetFiles(packsDir, "*.zip"))
                    {
                        try
                        {
                            using var archive = ZipFile.OpenRead(zipFile);
                            var langEntry = archive.GetEntry($"lang/{langCode}.json");
                            if (langEntry != null)
                            {
                                using var stream = langEntry.Open();
                                using var reader = new StreamReader(stream);
                                MergeTranslations(reader.ReadToEnd());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TranslationService] Błąd wczytywania języka z paczki zip {zipFile}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranslationService] Błąd skanowania folderu paczek dla tłumaczeń: {ex.Message}");
        }
    }

    private void MergeTranslations(string jsonContent)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
        if (dict != null)
        {
            foreach (var kvp in dict)
            {
                _translations[kvp.Key] = kvp.Value;
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
