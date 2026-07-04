using System.Collections.Generic;

namespace DungeonApp.Services;

public interface IStorageService
{
    List<T> LoadAll<T>(string directoryPath) where T : class;
    T? Load<T>(string filePath) where T : class;
    void Save<T>(string filePath, T data) where T : class;
    void Delete(string filePath);
    void DeleteDirectory(string directoryPath);
}
