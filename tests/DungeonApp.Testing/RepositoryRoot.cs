using System;
using System.IO;

namespace DungeonApp.Testing;

/// <summary>
/// Paths inside the checked-out repository, for tests that read files as they are committed rather
/// than as they were copied into the build output. Walking up to the solution file is the one anchor
/// that works both under <c>dotnet test</c> and from the IDE.
/// </summary>
public static class RepositoryRoot
{
    private const string SolutionFileName = "DungeonApp.sln";

    public static string Path { get; } = Locate();

    public static string SrcSources { get; } = System.IO.Path.Combine(Path, "src");

    public static string CoreSources { get; } = System.IO.Path.Combine(Path, "src", "DungeonApp.Core");

    public static string DesktopSources { get; } = System.IO.Path.Combine(Path, "src", "DungeonApp.Desktop");

    /// <summary>Reference packs shared by every test project that loads packs.</summary>
    public static string PackFixtures { get; } = System.IO.Path.Combine(Path, "tests", "Fixtures", "Packs");

    /// <summary>The packs shipped with the D&amp;D 5e system, as committed.</summary>
    public static string Dnd5eBundledPacks { get; } = System.IO.Path.Combine(Path, "src", "DungeonApp.Content.Dnd5e", "Packs");

    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"No '{SolutionFileName}' above '{AppContext.BaseDirectory}'.");
    }
}
