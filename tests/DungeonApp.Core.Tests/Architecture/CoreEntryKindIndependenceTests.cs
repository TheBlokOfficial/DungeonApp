using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DungeonApp.Core.Tests.Architecture;

/// <summary>
/// The twin of <see cref="CoreIndependenceTests"/>, guarding the second boundary this project
/// rests on: the engine does not know what kinds of things exist. There is no Monster and no
/// Spell, no enum of entry kind, and no branch keyed by one. An entry names a template, a template
/// names card elements, and that path is identical for every entry there will ever be.
/// <para>
/// This is a vocabulary scan, and it is worth being exact about what that buys. It cannot see a
/// comparison against an id read from a file; nothing mechanical can. What it does catch is the
/// way this boundary actually gives way in practice - somebody needs one special case, names a
/// type or a constant after the thing being special-cased, and the next reader takes that name as
/// permission. Denying the vocabulary denies the first step.
/// </para>
/// <para>
/// The forbidden words are mostly not hand-picked: they are read out of the pack fixtures, so the
/// engine is denied exactly the vocabulary the data introduces, and the ban widens by itself as
/// content is written. The short fixed list beside them only covers what an English-named
/// violation would reach for before any fixture names it.
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
    public void Core_does_not_name_any_entry_kind()
    {
        var forbidden = ForeseeableKinds.Concat(KindsNamedByFixtures())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var offences = (
            from file in EngineSourceFiles()
            let text = File.ReadAllText(file)
            from word in forbidden
            where text.Contains(word, StringComparison.OrdinalIgnoreCase)
            select $"{Path.GetRelativePath(RepositoryRoot.Path, file)} names '{word}'")
            .ToArray();

        Assert.True(
            offences.Length == 0,
            "DungeonApp.Core must not name an entry kind:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offences));
    }

    /// <summary>
    /// A scan that finds nothing because it looked nowhere is worse than no scan: it reports the
    /// boundary as held. Both halves are asserted so the check cannot pass by being empty.
    /// </summary>
    [Fact]
    public void The_scan_reaches_both_the_engine_and_the_fixtures()
    {
        Assert.NotEmpty(EngineSourceFiles());
        Assert.NotEmpty(KindsNamedByFixtures());
    }

    private static IReadOnlyList<string> EngineSourceFiles() =>
    [
        .. Directory
            .EnumerateFiles(RepositoryRoot.CoreSources, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file))
    ];

    /// <summary>
    /// Every name the packs introduce for a kind of thing: the templates that give entries their
    /// shape, and the entries themselves.
    /// </summary>
    private static IReadOnlyList<string> KindsNamedByFixtures()
    {
        var names = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     RepositoryRoot.PackFixtures, "*.json", SearchOption.AllDirectories))
        {
            var folder = Path.GetFileName(Path.GetDirectoryName(file));

            if (folder is not ("templates" or "entries"))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(file));

            foreach (var property in (string[])["id", "name"])
            {
                if (document.RootElement.TryGetProperty(property, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private static bool IsBuildOutput(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
