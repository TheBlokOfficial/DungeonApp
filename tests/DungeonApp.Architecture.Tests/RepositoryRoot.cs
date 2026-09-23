using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    /// <summary>Every project under <c>src/</c>, for the checks that enumerate systemy by file.</summary>
    public static string SrcSources { get; } =
        System.IO.Path.Combine(Path, "src");

    /// <summary>The engine's own sources, for the checks that read code rather than run it.</summary>
    public static string CoreSources { get; } =
        System.IO.Path.Combine(Path, "src", "DungeonApp.Core");

    /// <summary>The shell's own sources, scanned alongside the engine's for the same reason.</summary>
    public static string DesktopSources { get; } =
        System.IO.Path.Combine(Path, "src", "DungeonApp.Desktop");

    /// <summary>
    /// Every library project's own source root - one entry per <c>src/DungeonApp.Library.*</c>
    /// directory, discovered rather than named, so a library that is later split, renamed or added
    /// keeps the same vocabulary scan (docs/architecture.md, "Rama, biblioteka, system": a library
    /// "zna Entry, nie zna Monster") automatically, instead of quietly falling outside it the way a
    /// hardcoded single path would.
    /// </summary>
    public static IReadOnlyList<string> LibrarySourceRoots { get; } =
        Directory.EnumerateDirectories(SrcSources, "DungeonApp.Library.*", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// Every library project's simple assembly name - "DungeonApp.Library.Workspace",
    /// "DungeonApp.Library.Entries.Desktop" and so on - read from the <c>.csproj</c> files under
    /// <c>src/</c> rather than hardcoded, the same discovery <see cref="LibrarySourceRoots"/> uses,
    /// so the reference-boundary tests that load these assemblies by name cover a newly split or
    /// renamed library without needing to change.
    /// </summary>
    public static IReadOnlyList<string> LibraryAssemblyNames { get; } =
        Directory.EnumerateFiles(SrcSources, "DungeonApp.Library.*.csproj", SearchOption.AllDirectories)
            .Select(System.IO.Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

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
