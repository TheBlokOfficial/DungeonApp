using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DungeonApp.Models;

namespace DungeonApp.Services;

public class CharacterService
{
    private readonly string _baseDirectory;

    public CharacterService()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _baseDirectory = Path.Combine(documents, "DungeonSessionManager", "Characters");

        Directory.CreateDirectory(_baseDirectory);
    }

    public List<PlayerCharacter> LoadAllCharacters()
    {
        var characters = new List<PlayerCharacter>();

        foreach (var file in Directory.GetFiles(_baseDirectory, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var character = JsonSerializer.Deserialize<PlayerCharacter>(json);
                if (character != null)
                {
                    characters.Add(character);
                }
            }
            catch { /* pomijamy uszkodzone pliki */ }
        }

        // Sortujemy alfabetycznie po imieniu bohatera
        return characters.OrderBy(c => c.CharacterName).ToList();
    }

    public void SaveCharacter(PlayerCharacter character)
    {
        string filePath = Path.Combine(_baseDirectory, $"{character.Id}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(character, options);
        File.WriteAllText(filePath, json);
    }

    public void DeleteCharacter(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        string filePath = Path.Combine(_baseDirectory, $"{id}.json");
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Błąd usunięcia: {ex.Message}"); }
        }
    }
}