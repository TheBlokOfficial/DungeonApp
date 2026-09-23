using System;
using System.IO;

namespace DungeonApp.Library.Entries.Desktop.Tests;

/// <summary>
/// A throwaway packs directory on the real filesystem, shared by every Library.Entries.Desktop test that
/// needs <see cref="DungeonApp.Library.Entries.ContentPackLoader"/> to read real files rather than a
/// hand-built <see cref="DungeonApp.Library.Entries.ContentRegistry"/>.
/// <para>
/// Copied minimally from <c>DungeonApp.Desktop.Tests.TestPacks</c> rather than shared across test
/// projects (docs/tasks.md, etap 2): that class is <c>internal</c> and other Desktop.Tests fixtures
/// still need it, and a reference between two test projects would be a second, needless edge for the
/// sake of one small helper - the same call <c>DungeonApp.Architecture.Tests</c>' own
/// <c>RepositoryRoot</c> already made.
/// </para>
/// </summary>
public sealed class TestPacks : IDisposable
{
    public TestPacks()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-library-entries-desktop-packs-{Guid.NewGuid():N}");
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
