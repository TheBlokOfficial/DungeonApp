using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The downward half of "Rama, biblioteka, system" for the newest layer: the frame -
/// <c>DungeonApp.Core</c> and <c>DungeonApp.Desktop</c> - must not reference the shared UI library.
/// Strzałki idą tylko w dół (docs/architecture.md, "Rama, biblioteka, system"): the library takes a
/// project reference to the frame, never the other way around, and that direction is what lets a
/// system be the only thing standing on top of the library without the frame ever needing to know it
/// exists.
/// <para>
/// Matched by assembly-name prefix, the same way <see cref="ContentAssemblyReferenceTests"/> matches
/// "DungeonApp.Content." - this étape only builds <c>DungeonApp.Library.Desktop</c>, but the prefix
/// also denies a later, Avalonia-free <c>DungeonApp.Library</c> without this test needing to change.
/// </para>
/// </summary>
public sealed class LibraryAssemblyReferenceTests
{
    [Fact]
    public void Core_does_not_reference_the_library()
    {
        AssertReferencesNoLibraryAssembly(Assembly.Load("DungeonApp.Core"));
    }

    [Fact]
    public void Desktop_does_not_reference_the_library()
    {
        AssertReferencesNoLibraryAssembly(Assembly.Load("DungeonApp.Desktop"));
    }

    private static void AssertReferencesNoLibraryAssembly(Assembly assembly)
    {
        var libraryReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && name.StartsWith("DungeonApp.Library", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            libraryReferences.Length == 0,
            $"{assembly.GetName().Name} must not reference the library, but references: "
            + string.Join(", ", libraryReferences));
    }
}
