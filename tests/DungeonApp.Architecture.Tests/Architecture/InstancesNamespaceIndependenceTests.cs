using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// docs/tasks.md's brief for this étape: instances stay inside <c>DungeonApp.Core</c> for now, but
/// in their own namespace (<c>DungeonApp.Core.Content.Instances</c>), and the frame's own save code
/// and its single change entry point must not reference it. Nothing about instances is special to
/// the frame - <c>JsonCampaignRepository</c> saves and loads any state model a system declares
/// through <c>StateModelDeclaration</c>'s non-generic base, and <c>CampaignSession.ChangeAsync</c>
/// applies any <c>CampaignChange</c> - and this scan is what keeps that true instead of merely
/// intended: a reference to <c>CampaignInstance</c> or its namespace creeping into either file is
/// exactly the first step of the frame quietly learning what a system's own model is for.
/// </summary>
public sealed class InstancesNamespaceIndependenceTests
{
    private const string ForbiddenNamespace = "DungeonApp.Core.Content.Instances";

    [Fact]
    public void The_frames_save_code_does_not_reference_the_instances_namespace()
    {
        AssertNoReference(PersistenceSourceFiles());
    }

    [Fact]
    public void The_frames_single_change_entry_point_does_not_reference_the_instances_namespace()
    {
        AssertNoReference([CampaignSessionFile()]);
    }

    /// <summary>A scan that finds nothing because it looked nowhere is worse than no scan - see the sibling boundary tests for the same reasoning.</summary>
    [Fact]
    public void The_scan_reaches_persistence_and_the_change_entry_point()
    {
        var persistence = PersistenceSourceFiles();
        Assert.NotEmpty(persistence);
        Assert.Contains(persistence, file => file.EndsWith("JsonCampaignRepository.cs", StringComparison.OrdinalIgnoreCase));
        Assert.True(File.Exists(CampaignSessionFile()));
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
                var line = lines[lineIndex];

                if (line.Contains(ForbiddenNamespace, StringComparison.Ordinal)
                    || VocabularyWordBoundary.IsMatch(line, "CampaignInstance"))
                {
                    offences.Add($"{relativePath}:{lineIndex + 1}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "The frame's save code and its single change entry point must not reference the "
            + $"instances namespace or CampaignInstance, but found:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offences));
    }

    private static IReadOnlyList<string> PersistenceSourceFiles() =>
        Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot.CoreSources, "Persistence"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

    private static string CampaignSessionFile() =>
        Path.Combine(RepositoryRoot.DesktopSources, "Shell", "CampaignSession.cs");
}
