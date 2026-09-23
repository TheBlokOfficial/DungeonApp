using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Library.Entries.Instances;
using DungeonApp.Core.Persistence;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Proof that the on-disk campaign format has not moved by a single byte across the etap 4 brief 2
/// migration (docs/tasks.md, "Wyniesienie logiki wpisów z rdzenia do biblioteki"). The fixture under
/// <c>Fixtures/CampaignFormat</c> was generated once, by the pre-migration code, from a campaign
/// with a declared system and two instances - one carrying a GM-given label and a non-empty patch,
/// one plain - and is never regenerated or hand-edited afterwards; this test file itself may only
/// ever change in its <c>using</c> directives as types move namespace, never in what it asserts (the
/// brief's own condition for this test).
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

    [Fact]
    public async Task Loading_the_fixture_and_saving_it_fresh_reproduces_it_byte_for_byte()
    {
        var source = new JsonCampaignRepository(
            FixtureLibrary, path => throw new InvalidOperationException("The fixture must never be deleted."));

        var campaign = await source.GetAsync(new CampaignId(CampaignGuid), [InstancesModel.Declaration])
            ?? throw new InvalidOperationException("The campaign format fixture failed to load.");

        var destination = new JsonCampaignRepository(
            _destinationLibrary, path => Directory.Delete(path, recursive: true));

        await destination.SaveAsync(campaign, [InstancesModel.Declaration]);

        AssertDirectoriesMatchByteForByte(
            Path.Combine(FixtureLibrary, CampaignGuid.ToString("D")),
            Path.Combine(_destinationLibrary, CampaignGuid.ToString("D")));
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
