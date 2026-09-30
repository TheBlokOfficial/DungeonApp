using System;
using System.Collections.Generic;
using System.IO;

namespace DungeonApp.Testing;

/// <summary>A fresh directory under the system temp path, deleted on dispose.</summary>
public abstract class TemporaryDirectory : IDisposable
{
    protected TemporaryDirectory(string prefix)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

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

/// <summary>A temporary campaigns directory.</summary>
public sealed class TemporaryLibrary() : TemporaryDirectory("dungeonapp-tests")
{
    public string CampaignDirectory(Guid id) => System.IO.Path.Combine(Path, id.ToString("D"));

    public string DocumentPath(Guid id) => System.IO.Path.Combine(CampaignDirectory(id), "campaign.json");

    public void WriteDocument(Guid id, string json)
    {
        Directory.CreateDirectory(CampaignDirectory(id));
        File.WriteAllText(DocumentPath(id), json);
    }
}

/// <summary>A temporary packs directory: one subdirectory per pack.</summary>
public sealed class TemporaryPacks() : TemporaryDirectory("dungeonapp-packs")
{
    public string PackDirectory(string packDirectoryName) => System.IO.Path.Combine(Path, packDirectoryName);

    public void WriteFile(string packDirectoryName, string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(PackDirectory(packDirectoryName), relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void WritePack(string packDirectoryName, string packJson, IReadOnlyDictionary<string, string>? files = null)
    {
        WriteFile(packDirectoryName, "pack.json", packJson);

        foreach (var (relativePath, content) in files ?? new Dictionary<string, string>())
        {
            WriteFile(packDirectoryName, relativePath, content);
        }
    }
}
