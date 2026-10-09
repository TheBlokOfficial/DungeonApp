using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Content.Dnd5e;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.Persistence;
using DungeonApp.Core.State;
using DungeonApp.Core.World;
using DungeonApp.Testing;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Proof that the on-disk campaign format does not move by a single byte. The fixture under
/// <c>Fixtures/CampaignFormat</c> was generated from a campaign with a declared system, two world
/// folders and two entities - one in a folder with a GM-given label and a non-empty patch, one plain
/// in the root - and a counter ahead of the highest number. It is regenerated only when the format
/// changes on purpose (<c>Fixtures/LegacyCampaign</c> keeps the previous shape as a campaign that
/// must still open), never hand-edited.
/// <para>
/// The fixture directory is read from, never written to: <see cref="ICampaignRepository.GetAsync"/>
/// only reads, and the re-save goes to a fresh, empty temporary directory rather than back on top of
/// the fixture - <see cref="JsonCampaignRepository.SaveAsync"/> always bumps the save generation by
/// reading whatever manifest is already at the destination, so writing over the fixture itself would
/// bump generation 1 to 2 and never compare equal, regardless of whether the format actually moved.
/// A fresh destination has no previous manifest, so its first save lands at generation 1 - the same
/// generation the fixture itself was captured at.
/// </para>
/// </summary>
public sealed class CampaignFormatFixtureTests : IDisposable
{
    private static readonly Guid CampaignGuid = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly string _destinationLibrary;

    public CampaignFormatFixtureTests() =>
        _destinationLibrary = Path.Combine(Path.GetTempPath(), $"dungeonapp-format-fixture-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (!Directory.Exists(_destinationLibrary))
        {
            return;
        }

        try
        {
            Directory.Delete(_destinationLibrary, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a green test over.
        }
    }

    private static string FixtureLibrary => Path.Combine(
        RepositoryRoot.Path, "tests", "DungeonApp.Content.Dnd5e.Tests", "Fixtures", "CampaignFormat");

    private static string LegacyLibrary => Path.Combine(
        RepositoryRoot.Path, "tests", "DungeonApp.Content.Dnd5e.Tests", "Fixtures", "LegacyCampaign");

    /// <summary>The models every campaign of this system keeps: the frame's own, and none of the system's.</summary>
    private static IReadOnlyList<StateModelDeclaration> Declarations => WorldModels.Combine([]);

    [Fact]
    public async Task Loading_the_fixture_and_saving_it_fresh_reproduces_it_byte_for_byte()
    {
        var systemId = DungeonApp.Core.Systems.SystemId.Create(Dnd5eSystem.IdValue);

        var source = new JsonCampaignRepository(
            systemId, FixtureLibrary, path => throw new InvalidOperationException("The fixture must never be deleted."));

        var campaign = await source.GetAsync(new CampaignId(CampaignGuid), Declarations)
            ?? throw new InvalidOperationException("The campaign format fixture failed to load.");

        var destination = new JsonCampaignRepository(
            systemId, _destinationLibrary, path => Directory.Delete(path, recursive: true));

        await destination.SaveAsync(campaign, Declarations);

        AssertDirectoriesMatchByteForByte(
            Path.Combine(FixtureLibrary, CampaignGuid.ToString("D")),
            Path.Combine(_destinationLibrary, CampaignGuid.ToString("D")));
    }

    /// <summary>
    /// A campaign saved before folders and numbers existed (<c>Fixtures/LegacyCampaign</c>, never
    /// regenerated) still opens: it has no file for the new models, and its entities are numbered on
    /// read in the order of their ids, the same at every opening.
    /// </summary>
    [Fact]
    public async Task A_campaign_saved_before_folders_and_numbers_still_opens_with_stable_numbers()
    {
        var systemId = DungeonApp.Core.Systems.SystemId.Create(Dnd5eSystem.IdValue);
        var repository = new JsonCampaignRepository(
            systemId, LegacyLibrary, path => throw new InvalidOperationException("The fixture must never be deleted."));

        for (var opening = 0; opening < 2; opening++)
        {
            var campaign = await repository.GetAsync(new CampaignId(CampaignGuid), Declarations)
                ?? throw new InvalidOperationException("The legacy campaign fixture failed to load.");

            var entities = campaign.Snapshot.Get(EntitiesModel.Declaration);
            Assert.Equal(1, entities["27a5bad0-d2d5-4a08-9917-3306acd1832a"].Number);
            Assert.Equal(2, entities["f2f659c8-208a-4a0a-b62e-5768fb336401"].Number);
            Assert.All(entities.Values, entity => Assert.Null(entity.FolderId));
            Assert.Empty(campaign.Snapshot.Get(WorldModels.Folders));
            Assert.Equal(2, WorldNumbering.LastNumber(campaign.Snapshot));
        }
    }

    private static void AssertDirectoriesMatchByteForByte(string expectedRoot, string actualRoot)
    {
        var expectedFiles = RelativeFiles(expectedRoot);
        var actualFiles = RelativeFiles(actualRoot);

        Assert.Equal(expectedFiles, actualFiles);

        foreach (var relative in expectedFiles)
        {
            var expectedBytes = File.ReadAllBytes(Path.Combine(expectedRoot, relative));
            var actualBytes = File.ReadAllBytes(Path.Combine(actualRoot, relative));

            Assert.True(
                expectedBytes.AsSpan().SequenceEqual(actualBytes),
                $"'{relative}' is not byte-for-byte identical to the fixture.");
        }
    }

    private static string[] RelativeFiles(string root) =>
        [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file))
            .OrderBy(relative => relative, StringComparer.Ordinal)];
}
