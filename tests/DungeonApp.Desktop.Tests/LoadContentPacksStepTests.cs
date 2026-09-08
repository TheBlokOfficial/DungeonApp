using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Startup;

namespace DungeonApp.Desktop.Tests;

public sealed class LoadContentPacksStepTests : IDisposable
{
    private readonly TestPacks _packs = new();

    private const string SystemPackJson = """
        {
          "formatVersion": 1,
          "id": "sys",
          "kind": "system",
          "name": "System",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    private const string TemplateJson = """
        {
          "id": "thing",
          "name": "Thing",
          "version": 1,
          "catalogVersion": 1,
          "fields": [
            { "id": "label", "label": "Label", "type": "text" }
          ],
          "card": [
            { "element": "statblock", "traits": [ { "field": "label" } ] }
          ]
        }
        """;

    private const string ContentPackJson = """
        {
          "formatVersion": 1,
          "id": "content",
          "kind": "content",
          "name": "Content",
          "version": { "major": 1, "minor": 0 }
        }
        """;

    private const string EntryJson = """
        {
          "id": "e1",
          "name": "Entry One",
          "template": "sys:thing",
          "templateVersion": 1,
          "values": { "label": "A" }
        }
        """;

    public void Dispose() => _packs.Dispose();

    [Fact]
    public async Task PrepareAsync_populates_registry_with_system_pack_and_resolved_entry()
    {
        _packs.WriteFile("sys", "pack.json", SystemPackJson);
        _packs.WriteFile("sys", "templates/thing.json", TemplateJson);
        _packs.WriteFile("content", "pack.json", ContentPackJson);
        _packs.WriteFile("content", "entries/e1.json", EntryJson);

        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Empty(step.Registry.RejectedPacks);
        Assert.Single(step.Registry.SystemPacks);
        var registered = Assert.Single(step.Registry.Entries);
        Assert.Null(registered.Unresolved);
        Assert.NotNull(registered.Template);
    }

    // The single most important test here: a broken pack must never keep a good sibling pack, or
    // startup itself, from succeeding.
    [Fact]
    public async Task PrepareAsync_does_not_throw_when_one_pack_is_malformed_and_keeps_the_valid_one()
    {
        _packs.WriteFile("sys", "pack.json", SystemPackJson);
        _packs.WriteFile("sys", "templates/thing.json", TemplateJson);
        _packs.WriteFile("broken", "pack.json", "{ this is not json");

        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Single(step.Registry.SystemPacks);
        var rejected = Assert.Single(step.Registry.RejectedPacks);
        Assert.Contains("broken", rejected.Location);
    }

    [Fact]
    public async Task PrepareAsync_does_not_throw_when_packs_directory_is_missing()
    {
        var missingPath = Path.Combine(_packs.Path, "does-not-exist");
        var step = new LoadContentPacksStep(new ContentPackLoader(missingPath));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Empty(step.Registry.SystemPacks);
        Assert.Empty(step.Registry.ContentPacks);
        Assert.Empty(step.Registry.Entries);
        Assert.Empty(step.Registry.RejectedPacks);
    }

    [Fact]
    public void Registry_is_an_empty_registry_before_PrepareAsync_runs()
    {
        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path));

        Assert.NotNull(step.Registry);
        Assert.Empty(step.Registry.SystemPacks);
        Assert.Empty(step.Registry.ContentPacks);
        Assert.Empty(step.Registry.Entries);
        Assert.Empty(step.Registry.RejectedPacks);
    }

    [Fact]
    public void Describe_returns_a_non_empty_message()
    {
        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path));

        Assert.False(string.IsNullOrWhiteSpace(step.Describe()));
    }

    /// <summary>
    /// A throwaway packs directory on the real filesystem, kept local to this test file because the
    /// Desktop test project does not reference DungeonApp.Core.Tests and this task adds no such
    /// reference. Mirrors <c>DungeonApp.Core.Tests.Fakes.TemporaryPacks</c> in spirit, trimmed to
    /// what these tests need.
    /// </summary>
    private sealed class TestPacks : IDisposable
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
}
