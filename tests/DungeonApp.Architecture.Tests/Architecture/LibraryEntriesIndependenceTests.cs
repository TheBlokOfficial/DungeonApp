using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// docs/architecture.md, "Granice mechaniczne": "logika bibliotek bez Avalonii ... powstaje razem z
/// pierwszą biblioteką bez Avalonii" - <c>DungeonApp.Library.Entries</c> is that first library, and
/// this is its guard, the same shape as <see cref="CoreIndependenceTests"/> for the engine.
/// <para>
/// Named to one project rather than discovered by scanning every <c>DungeonApp.Library.*</c> project
/// that lacks a <c>.Desktop</c> suffix: <c>DungeonApp.Library.Workspace</c> also lacks that suffix
/// and legitimately does reference Avalonia (it owns the desk's own windows), so a blanket
/// "no suffix means no Avalonia" scan would misfire on it the day this test is introduced. The rule
/// this repository actually follows is narrower - a library that carries no view has no reason to
/// reference Avalonia, and today <c>DungeonApp.Library.Entries</c> is the only library of that kind.
/// Widen this into a scan (the way <see cref="LibraryAssemblyReferenceTests"/> does) only once a
/// second Avalonia-free library exists to prove the pattern.
/// </para>
/// </summary>
public sealed class LibraryEntriesIndependenceTests
{
    [Fact]
    public void Library_Entries_does_not_reference_Avalonia()
    {
        var library = Assembly.Load("DungeonApp.Library.Entries");

        var uiReferences = library
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && name.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(uiReferences);
    }
}
