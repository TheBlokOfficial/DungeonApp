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

    private static readonly ContentTypeReference ThingReference =
        new(ContentId.Create("sys"), ContentId.Create("thing"));

    private static FakeContentSet ThingContentSet() =>
        new(ContentId.Create("sys"), [new ContentTypeDescriptor(ThingReference, "Thing", 1)]);

    private const string PackJson = """
        {
          "formatVersion": 1,
          "id": "content",
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
    public async Task PrepareAsync_populates_registry_with_pack_and_resolved_entry()
    {
        _packs.WriteFile("content", "pack.json", PackJson);
        _packs.WriteFile("content", "entries/e1.json", EntryJson);

        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path, ThingContentSet()));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Empty(step.Registry.RejectedPacks);
        Assert.Single(step.Registry.Packs);
        var registered = Assert.Single(step.Registry.Entries);
        Assert.Null(registered.Unresolved);
        Assert.NotNull(registered.Type);
    }

    // The single most important test here: a broken pack must never keep a good sibling pack, or
    // startup itself, from succeeding.
    [Fact]
    public async Task PrepareAsync_does_not_throw_when_one_pack_is_malformed_and_keeps_the_valid_one()
    {
        _packs.WriteFile("good", "pack.json", PackJson.Replace("\"content\"", "\"good\""));
        _packs.WriteFile("good", "entries/e1.json", EntryJson);
        _packs.WriteFile("broken", "pack.json", "{ this is not json");

        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path, ThingContentSet()));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Single(step.Registry.Packs);
        var rejected = Assert.Single(step.Registry.RejectedPacks);
        Assert.Contains("broken", rejected.Location);
    }

    [Fact]
    public async Task PrepareAsync_does_not_throw_when_packs_directory_is_missing()
    {
        var missingPath = Path.Combine(_packs.Path, "does-not-exist");
        var step = new LoadContentPacksStep(new ContentPackLoader(missingPath, ThingContentSet()));

        await step.PrepareAsync(CancellationToken.None);

        Assert.Empty(step.Registry.Packs);
        Assert.Empty(step.Registry.Entries);
        Assert.Empty(step.Registry.RejectedPacks);
    }

    [Fact]
    public void Registry_is_an_empty_registry_before_PrepareAsync_runs()
    {
        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path, ThingContentSet()));

        Assert.NotNull(step.Registry);
        Assert.Empty(step.Registry.Packs);
        Assert.Empty(step.Registry.Entries);
        Assert.Empty(step.Registry.RejectedPacks);
    }

    [Fact]
    public void Describe_returns_a_non_empty_message()
    {
        var step = new LoadContentPacksStep(new ContentPackLoader(_packs.Path, ThingContentSet()));

        Assert.False(string.IsNullOrWhiteSpace(step.Describe()));
    }
}
