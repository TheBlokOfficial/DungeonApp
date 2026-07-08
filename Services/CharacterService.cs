using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonApp.Models;

namespace DungeonApp.Services;

public interface ICharacterService
{
    List<PlayerCharacter> LoadAllCharacters();
    void SaveCharacter(PlayerCharacter character);
    void DeleteCharacter(string id);
}

public class CharacterService : ICharacterService
{
    private readonly string _baseDirectory;
    private readonly IStorageService _storageService;

    public CharacterService(IStorageService storageService)
    {
        _storageService = storageService;
        _baseDirectory = Path.Combine(AppPaths.UserDataPath, "characters");
        Directory.CreateDirectory(_baseDirectory);
    }

    public List<PlayerCharacter> LoadAllCharacters()
    {
        var characters = _storageService.LoadAll<PlayerCharacter>(_baseDirectory);
        return characters.OrderBy(c => c.CharacterName).ToList();
    }

    public void SaveCharacter(PlayerCharacter character)
    {
        if (character == null) return;
        string filePath = Path.Combine(_baseDirectory, $"{character.Id}.json");
        _storageService.Save(filePath, character);
    }

    public void DeleteCharacter(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        string filePath = Path.Combine(_baseDirectory, $"{id}.json");
        _storageService.Delete(filePath);
    }
}
