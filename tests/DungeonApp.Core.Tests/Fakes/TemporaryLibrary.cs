using System;
using System.IO;

namespace DungeonApp.Core.Tests.Fakes;

/// <summary>
/// A throwaway campaign library on the real filesystem. The persistence tests exercise the actual
/// atomic move and the actual directory layout, so faking the filesystem here would test nothing.
/// </summary>
internal sealed class TemporaryLibrary : IDisposable
{
    public TemporaryLibrary()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dungeonapp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CampaignDirectory(Guid id) => System.IO.Path.Combine(Path, id.ToString("D"));

    public string DocumentPath(Guid id) => System.IO.Path.Combine(CampaignDirectory(id), "campaign.json");

    /// <summary>Plants a document the repository did not write, to test how it reacts to one.</summary>
    public void WriteDocument(Guid id, string json)
    {
        Directory.CreateDirectory(CampaignDirectory(id));
        File.WriteAllText(DocumentPath(id), json);
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
