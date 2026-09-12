using System;
using System.Collections.Generic;
using System.IO;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// A throwaway packs directory on the real filesystem, for the content loader tests. Mirrors
/// <see cref="TemporaryLibrary"/>: real IO, no fake filesystem, because the loader's own directory
/// scanning and file reads are exactly what these tests exercise.
/// <para>
/// Unlike the loader itself, this type happily writes a pack the loader would never produce on its
/// own - a missing <c>pack.json</c>, a malformed one, a stray directory - because that is exactly
/// the shape the rejection tests need to plant.
/// </para>
/// </summary>
internal sealed class TemporaryPacks : IDisposable
{
    public TemporaryPacks()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-packs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    /// <summary>The directory to hand a <c>ContentPackLoader</c> as its <c>packsPath</c>.</summary>
    public string Path { get; }

    public string PackDirectory(string packDirectoryName) => System.IO.Path.Combine(Path, packDirectoryName);

    /// <summary>Writes one file, raw, inside a pack directory - creating both as needed.</summary>
    public void WriteFile(string packDirectoryName, string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(PackDirectory(packDirectoryName), relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    /// <summary>Convenience for the common case: a <c>pack.json</c> plus zero or more other files.</summary>
    public void WritePack(string packDirectoryName, string packJson, IReadOnlyDictionary<string, string>? files = null)
    {
        WriteFile(packDirectoryName, "pack.json", packJson);

        foreach (var (relativePath, content) in files ?? new Dictionary<string, string>())
        {
            WriteFile(packDirectoryName, relativePath, content);
        }
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
