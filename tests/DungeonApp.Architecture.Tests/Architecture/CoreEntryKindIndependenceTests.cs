using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The twin of <see cref="CoreIndependenceTests"/>, guarding the second boundary this project
/// rests on: neither the engine nor the shell knows what kinds of things exist. There is no Monster
/// and no Spell, no enum of entry kind, and no branch keyed by one. An entry names a content type,
/// a content type names controls, and that path is identical for every entry there will ever be.
/// <para>
/// This is a vocabulary scan, and it is worth being exact about what that buys. It cannot see a
/// comparison against an id read from a file; nothing mechanical can. What it does catch is the
/// way this boundary actually gives way in practice - somebody needs one special case, names a
/// type, a view, or a constant after the thing being special-cased, and the next reader takes that
/// name as permission. Denying the vocabulary denies the first step.
/// </para>
/// <para>
/// The forbidden words are mostly not hand-picked: they are read out of the public type names of
/// every loaded <c>DungeonApp.Content.*</c> assembly, so the engine and the shell are denied
/// exactly the vocabulary the content layer introduces, and the ban widens by itself as content
/// types are written. The short fixed list beside them only covers what an English-named violation
/// would reach for before any zestaw names it.
/// </para>
/// <para>
/// Field names are deliberately not part of this dictionary. "name", "type", "size", "description"
/// are ordinary words that also occur throughout plumbing code that knows nothing about the game -
/// scanning for them produces false alarms from the first run, and a scan that cries wolf gets
/// switched off, at which point it guards nothing. The field-name boundary is enforced a different
/// way: values travel into the content layer as an envelope, and neither Core nor Desktop has any
/// API that accepts a field name as a string - there is nothing to ask.
/// </para>
/// </summary>
public sealed class CoreEntryKindIndependenceTests
{
    /// <summary>
    /// Deliberately short, and deliberately free of words that also read as plumbing: "item" is
    /// absent because a loop variable is not an architecture violation, and a check that cries
    /// wolf gets suppressed rather than heeded.
    /// </summary>
    private static readonly string[] ForeseeableKinds =
    [
        "monster", "creature", "spell", "weapon", "armor", "armour", "potion", "npc"
    ];

    [Fact]
    public void Engine_and_shell_do_not_name_any_entry_kind()
    {
        var forbidden = ForeseeableKinds
            .Concat(KindsNamedByContentAssemblies())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var offences = new List<string>();

        foreach (var file in ScannedSourceFiles())
        {
            var relativePath = Path.GetRelativePath(RepositoryRoot.Path, file);
            var lines = File.ReadAllLines(file);

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                foreach (var word in forbidden)
                {
                    if (VocabularyWordBoundary.IsMatch(lines[lineIndex], word))
                    {
                        offences.Add($"{relativePath}:{lineIndex + 1} names '{word}'");
                    }
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "DungeonApp.Core and DungeonApp.Desktop must not name an entry kind:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offences));
    }

    /// <summary>
    /// A scan that finds nothing because it looked nowhere is worse than no scan: it reports the
    /// boundary as held. Every half is asserted so the check cannot pass by being empty - including
    /// the halves that would silently vanish if someone dropped the Desktop side of the scan, or the
    /// <c>.axaml</c> extension, during a later edit.
    /// </summary>
    [Fact]
    public void The_scan_reaches_engine_shell_and_the_content_dictionary()
    {
        var files = ScannedSourceFiles();

        Assert.NotEmpty(files);
        Assert.Contains(
            files,
            file => file.StartsWith(RepositoryRoot.CoreSources, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            files,
            file => file.StartsWith(RepositoryRoot.DesktopSources, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            files,
            file => file.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(KindsNamedByContentAssemblies());
    }

    private static IReadOnlyList<string> ScannedSourceFiles() =>
    [
        .. SourceFiles(RepositoryRoot.CoreSources),
        .. SourceFiles(RepositoryRoot.DesktopSources)
    ];

    private static IEnumerable<string> SourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories))
            .Where(file => !IsBuildOutput(file));

    /// <summary>
    /// Every name a content assembly introduces for a kind of thing: today <c>Monster</c> and
    /// <c>Gear</c> from DungeonApp.Content.Dnd5e. Touching <see cref="Content.Dnd5e.Monster"/> forces
    /// that assembly to be loaded before the <see cref="AppDomain"/> is scanned - this project
    /// carries a project reference to it precisely so that touch is possible.
    /// </summary>
    private static IReadOnlyList<string> KindsNamedByContentAssemblies()
    {
        _ = typeof(Content.Dnd5e.Monster);

        return
        [
            .. AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name?.StartsWith(
                    "DungeonApp.Content.", StringComparison.Ordinal) == true)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsPublic)
                .Select(type => type.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static bool IsBuildOutput(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
