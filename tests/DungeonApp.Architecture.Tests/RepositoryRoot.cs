using System;
using System.IO;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// Finds the repository from inside a test binary, for the tests that need files as they are
/// checked in rather than as they were copied into the output.
/// <para>
/// Walking up to the solution file is the only anchor that survives both <c>dotnet test</c> and a
/// run from the IDE, which disagree about the working directory.
/// </para>
/// <para>
/// Duplicated from <c>DungeonApp.Core.Tests</c> rather than shared: that project still uses its own
/// copy (see <c>ContentPackLoaderTests</c>), and a reference between two test projects would be a
/// second, needless edge for the sake of a handful of path constants.
/// </para>
/// </summary>
internal static class RepositoryRoot
{
    private const string SolutionFileName = "DungeonApp.sln";

    public static string Path { get; } = Locate();

    /// <summary>Every project under <c>src/</c>, for the checks that enumerate zestawy by file.</summary>
    public static string SrcSources { get; } =
        System.IO.Path.Combine(Path, "src");

    /// <summary>The engine's own sources, for the checks that read code rather than run it.</summary>
    public static string CoreSources { get; } =
        System.IO.Path.Combine(Path, "src", "DungeonApp.Core");

    /// <summary>The shell's own sources, scanned alongside the engine's for the same reason.</summary>
    public static string DesktopSources { get; } =
        System.IO.Path.Combine(Path, "src", "DungeonApp.Desktop");

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

        throw new InvalidOperationException(
            $"No '{SolutionFileName}' above '{AppContext.BaseDirectory}'.");
    }
}
