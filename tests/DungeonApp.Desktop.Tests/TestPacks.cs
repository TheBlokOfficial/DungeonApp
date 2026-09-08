using System;
using System.IO;

namespace DungeonApp.Desktop.Tests;

/// <summary>
/// A throwaway packs directory on the real filesystem, shared by every Desktop test that needs
/// <see cref="DungeonApp.Core.Content.ContentPackLoader"/> to read real files rather than a
/// hand-built <see cref="DungeonApp.Core.Content.ContentRegistry"/>. Mirrors
/// <c>DungeonApp.Core.Tests.Fakes.TemporaryPacks</c> in spirit, trimmed to what these tests need -
/// the Desktop test project deliberately does not reference DungeonApp.Core.Tests.
/// </summary>
public sealed class TestPacks : IDisposable
{
    public TestPacks()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-desktop-packs-{Guid.NewGuid():N}");
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
