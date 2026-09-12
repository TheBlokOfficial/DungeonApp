using System;
using System.IO;

namespace DungeonApp.Content.Dnd5e.Tests;

/// <summary>
/// Finds the repository from inside a test binary. Duplicated from
/// <c>DungeonApp.Core.Tests</c>/<c>DungeonApp.Architecture.Tests</c> rather than shared - a reference
/// between test projects would be a needless edge for the sake of a handful of path constants.
/// </summary>
internal static class RepositoryRoot
{
    private const string SolutionFileName = "DungeonApp.sln";

    public static string Path { get; } = Locate();

    /// <summary>
    /// The Core test project's hand-written packs. Reused here rather than duplicated: they are
    /// already written against the real <c>dnd5e:monster</c>/<c>dnd5e:gear</c> shapes, so this
    /// project's tests can deserialize their entries straight into <see cref="Monster"/> and
    /// <see cref="Gear"/>.
    /// </summary>
    public static string PackFixtures { get; } =
        System.IO.Path.Combine(Path, "tests", "DungeonApp.Core.Tests", "Packs");

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
