using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The downward half of "Warstwy i granice": DungeonApp.Core and DungeonApp.Desktop must not
/// reference any content assembly. A system is the only place in the app allowed to know what a
/// Monster or a Gear is; if the engine or the shell referenced one, that knowledge would reach them
/// at compile time regardless of what <see cref="CoreEntryKindIndependenceTests"/>' vocabulary scan
/// finds in the source text.
/// </summary>
public sealed class ContentAssemblyReferenceTests
{
    [Fact]
    public void Core_does_not_reference_any_content_assembly()
    {
        AssertReferencesNoContentAssembly(Assembly.Load("DungeonApp.Core"));
    }

    [Fact]
    public void Desktop_does_not_reference_any_content_assembly()
    {
        AssertReferencesNoContentAssembly(Assembly.Load("DungeonApp.Desktop"));
    }

    /// <summary>
    /// Every library is common UI code, not a system (docs/architecture.md, "Rama, biblioteka,
    /// system": a library "zna Entry, nie zna Monster") - each must be exactly as ignorant of any
    /// content assembly as the engine and the shell are. Discovered by scanning <c>src/</c> for
    /// <c>DungeonApp.Library.*.csproj</c> (see <see cref="RepositoryRoot.LibraryAssemblyNames"/>)
    /// rather than named, so a library that is later split or renamed stays covered.
    /// </summary>
    [Fact]
    public void No_library_assembly_references_any_content_assembly()
    {
        var libraryAssemblyNames = RepositoryRoot.LibraryAssemblyNames;
        Assert.NotEmpty(libraryAssemblyNames);

        foreach (var name in libraryAssemblyNames)
        {
            AssertReferencesNoContentAssembly(Assembly.Load(name));
        }
    }

    private static void AssertReferencesNoContentAssembly(Assembly assembly)
    {
        var contentReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && name.StartsWith("DungeonApp.Content.", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            contentReferences.Length == 0,
            $"{assembly.GetName().Name} must not reference a content assembly, but references: "
            + string.Join(", ", contentReferences));
    }
}
