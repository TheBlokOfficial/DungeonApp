using System;
using System.IO;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// A throwaway packs directory on the real filesystem, for the two tests here that need a
/// deliberately broken entry rather than one of the checked-in fixtures. Mirrors
/// <c>DungeonApp.Core.Tests.Fakes.TemporaryPacks</c> and <c>DungeonApp.Desktop.Tests.TestPacks</c>,
/// trimmed to what this project needs.
/// </summary>
internal sealed class TemporaryPacks : IDisposable
{
    public TemporaryPacks()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-dnd5e-packs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void WriteFile(string packDirectoryName, string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, packDirectoryName, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a green test over.
        }
    }
}
