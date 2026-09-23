using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// TYMCZASOWY. Cel zlecenia "odchudzenie kontraktu między ramą a systemem"
/// (docs/tasks.md/briefu): po tym etapie żaden plik ramy nie używa logiki wpisów - nic w
/// <c>src/DungeonApp.Desktop/**</c>, w <c>src/DungeonApp.Core/**</c> poza <c>Core/Content/**</c>, ani w
/// <c>src/DungeonApp.Library.Workspace/**</c> (biblioteki się nie znają) nie odwołuje się do
/// przestrzeni nazw <c>DungeonApp.Core.Content</c> (włącznie z podprzestrzeniami, np.
/// <c>DungeonApp.Core.Content.Instances</c>). Logika wpisów zostaje fizycznie w <c>Core/Content</c> -
/// ten test nie skanuje jej samej.
/// <para>
/// Wzorowany na <see cref="InstancesNamespaceIndependenceTests"/>, tego samego kształtu skanu
/// słownictwa co <see cref="CoreEntryKindIndependenceTests"/>: dopasowanie po prostej podciągowości
/// (<c>Contains</c>, nie granica słowa) wystarcza tutaj, bo szukany ciąg jest już w pełni
/// kwalifikowaną nazwą przestrzeni nazw, nie pojedynczym słowem, które mogłoby się omyłkowo trafić
/// w innym kontekście.
/// </para>
/// <para>
/// Zastąpi go następne zlecenie testem po referencjach projektów ("rama nie referencuje biblioteki",
/// docs/tasks.md) - dziś biblioteka wpisów (<c>Library.Entries.Desktop</c>) fizycznie zawiera
/// <c>Core/Content</c>'ową logikę wpisów w swoim kodzie (rejestr, karty), więc granica projektowa nie
/// istnieje jeszcze; granica słownictwa w źródłach jest tym, co da się dziś sprawdzić mechanicznie.
/// </para>
/// </summary>
public sealed class ContentNamespaceIndependenceTests
{
    private const string ForbiddenNamespace = "DungeonApp.Core.Content";

    [Fact]
    public void The_frames_desktop_sources_do_not_reference_the_content_namespace()
    {
        AssertNoReference(SourceFiles(RepositoryRoot.DesktopSources));
    }

    [Fact]
    public void The_frames_core_sources_outside_Content_do_not_reference_the_content_namespace()
    {
        AssertNoReference(CoreSourcesOutsideContent());
    }

    [Fact]
    public void Library_Workspace_does_not_reference_the_content_namespace()
    {
        var workspaceRoot = RepositoryRoot.LibrarySourceRoots
            .Single(root => root.EndsWith("DungeonApp.Library.Workspace", StringComparison.OrdinalIgnoreCase));

        AssertNoReference(SourceFiles(workspaceRoot));
    }

    /// <summary>A scan that finds nothing because it looked nowhere is worse than no scan - see the sibling boundary tests for the same reasoning.</summary>
    [Fact]
    public void The_scan_reaches_desktop_core_outside_content_and_library_workspace()
    {
        var desktop = SourceFiles(RepositoryRoot.DesktopSources);
        Assert.NotEmpty(desktop);
        Assert.Contains(desktop, file => file.EndsWith("App.axaml.cs", StringComparison.OrdinalIgnoreCase));

        var coreOutsideContent = CoreSourcesOutsideContent();
        Assert.NotEmpty(coreOutsideContent);
        Assert.Contains(coreOutsideContent, file => file.EndsWith("JsonCampaignRepository.cs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            coreOutsideContent,
            file => file.Contains($"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        var workspaceRoot = RepositoryRoot.LibrarySourceRoots
            .Single(root => root.EndsWith("DungeonApp.Library.Workspace", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(SourceFiles(workspaceRoot));
    }

    private static void AssertNoReference(IReadOnlyList<string> files)
    {
        var offences = new List<string>();

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(RepositoryRoot.Path, file);
            var lines = File.ReadAllLines(file);

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (lines[lineIndex].Contains(ForbiddenNamespace, StringComparison.Ordinal))
                {
                    offences.Add($"{relativePath}:{lineIndex + 1}: {lines[lineIndex].Trim()}");
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            $"Must not reference the '{ForbiddenNamespace}' namespace, but found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offences));
    }

    private static IReadOnlyList<string> CoreSourcesOutsideContent() =>
        SourceFiles(RepositoryRoot.CoreSources)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static IReadOnlyList<string> SourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories))
            .Where(file => !IsBuildOutput(file))
            .ToArray();

    private static bool IsBuildOutput(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
