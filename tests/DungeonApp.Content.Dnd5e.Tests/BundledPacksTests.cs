using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DungeonApp.Core.Systems;
using DungeonApp.Library.Entries;
using DungeonApp.Library.Workspace.Features.CampaignWorkspace.Layout;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// The packs this system ships with the program land, after the build, exactly where the
/// composition root looks for them - <see cref="SystemDirectories.BundledPacks"/> under the output
/// directory, keyed by <see cref="Dnd5eSystem.IdValue"/>. The project file names that directory a
/// second time (MSBuild cannot read the C# constant), and a mismatch would be invisible in the app:
/// an empty or missing bundled directory is not an error, it is just nothing loaded.
/// </summary>
public sealed class BundledPacksTests
{
    private static string BundledPacksPath =>
        SystemDirectories.BundledPacks(AppContext.BaseDirectory, SystemId.Create(Dnd5eSystem.IdValue));

    [Fact]
    public async Task The_srd_pack_is_copied_where_the_program_reads_bundled_packs_and_loads_cleanly()
    {
        var system = new Dnd5eSystem(
            new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"dnd5e-bundled-tests-{Guid.NewGuid():N}")),
            [BundledPacksPath]);

        var registry = await new ContentPackLoader(BundledPacksPath, system).LoadAsync();

        Assert.Empty(registry.RejectedPacks);
        Assert.Empty(registry.RejectedEntries);
        var pack = Assert.Single(registry.Packs);
        Assert.Equal("dnd5e-srd", pack.Id.Value);
        Assert.Equal("SRD 5.1 — wpisy przykładowe", pack.Name);
        Assert.Empty(pack.Entries);
        Assert.True(File.Exists(Path.Combine(BundledPacksPath, "dnd5e-srd", "LICENSE.txt")));
    }
}
