using System.IO;
using DungeonApp.Core.Systems;

namespace DungeonApp.Core.Tests.Systems;

/// <summary>
/// docs/architecture.md, "Gdzie mieszka stan": the three directories a compiled system's own
/// documents live under, computed from an arbitrary root and a <see cref="SystemId"/> - never from
/// the real Documents folder or the real application base directory, so this never touches the
/// author's own machine.
/// </summary>
public sealed class SystemDirectoriesTests
{
    private static readonly SystemId Dnd5e = SystemId.Create("dnd5e");

    [Fact]
    public void GmPacks_sits_under_DungeonApp_and_the_systems_own_packs_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "dungeonapp-tests-documents");

        var path = SystemDirectories.GmPacks(root, Dnd5e);

        Assert.Equal(Path.Combine(root, "DungeonApp", "dnd5e", "packs"), path);
    }

    [Fact]
    public void Campaigns_sits_under_DungeonApp_and_the_systems_own_campaigns_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "dungeonapp-tests-documents");

        var path = SystemDirectories.Campaigns(root, Dnd5e);

        Assert.Equal(Path.Combine(root, "DungeonApp", "dnd5e", "campaigns"), path);
    }

    /// <summary>
    /// Bundled packs sit beside the program, not under the user's documents: no "DungeonApp" segment,
    /// and the root is the application's own base directory instead of Documents.
    /// </summary>
    [Fact]
    public void BundledPacks_sits_directly_under_the_base_directory_and_the_systems_own_packs_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "dungeonapp-tests-appdir");

        var path = SystemDirectories.BundledPacks(root, Dnd5e);

        Assert.Equal(Path.Combine(root, "dnd5e", "packs"), path);
    }

    [Fact]
    public void Different_system_ids_produce_different_directories()
    {
        var root = Path.Combine(Path.GetTempPath(), "dungeonapp-tests-documents");
        var other = SystemId.Create("other-system");

        Assert.NotEqual(SystemDirectories.GmPacks(root, Dnd5e), SystemDirectories.GmPacks(root, other));
        Assert.NotEqual(SystemDirectories.Campaigns(root, Dnd5e), SystemDirectories.Campaigns(root, other));
        Assert.NotEqual(SystemDirectories.BundledPacks(root, Dnd5e), SystemDirectories.BundledPacks(root, other));
    }
}
