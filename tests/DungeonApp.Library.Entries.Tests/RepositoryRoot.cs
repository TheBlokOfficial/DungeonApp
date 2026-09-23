using System;
using System.IO;

namespace DungeonApp.Library.Entries.Tests;

/// <summary>
/// Finds the repository from inside a test binary, for the tests that need files as they are
/// checked in rather than as they were copied into the output.
/// <para>
/// Walking up to the solution file is the only anchor that survives both <c>dotnet test</c> and a
/// run from the IDE, which disagree about the working directory.
/// </para>
/// </summary>
internal static class RepositoryRoot
{
    private const string SolutionFileName = "DungeonApp.sln";

    public static string Path { get; } = Locate();

    /// <summary>The hand-written packs. They stand in for a specification until one is written, so they are
    /// read by tests rather than treated as sample data.
    /// </summary>
    public static string PackFixtures { get; } =
        System.IO.Path.Combine(Path, "tests", "DungeonApp.Library.Entries.Tests", "Packs");

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
