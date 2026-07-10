using System;
using System.IO;

namespace DungeonApp.Services;

public static class AppPaths
{
    private const string AppDirectoryName = "DungeonApp";

    /// <summary>
    /// Zwraca ścieżkę do folderu instalacyjnego aplikacji.
    /// Zazwyczaj jest to AppDomain.CurrentDomain.BaseDirectory.
    /// </summary>
    public static string AppInstallationPath => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Zwraca ścieżkę do folderu wbudowanych paczek aplikacji (np. Core).
    /// </summary>
    public static string BuiltInPacksPath
    {
        get
        {
            string path = Path.Combine(AppInstallationPath, "packs");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Zwraca ścieżkę do folderu z danymi użytkownika (Kampanie, Postacie).
    /// Lokalizacja: C:\Users\[User]\Documents\DungeonApp\
    /// </summary>
    public static string UserDataPath
    {
        get
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = Path.Combine(documents, AppDirectoryName);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    /// <summary>
    /// Zwraca ścieżkę do folderu z ustawieniami i danymi aplikacji (Ukryte ustawienia).
    /// Lokalizacja: C:\Users\[User]\AppData\Roaming\DungeonApp\
    /// </summary>
    public static string AppDataPath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = Path.Combine(appData, AppDirectoryName);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
