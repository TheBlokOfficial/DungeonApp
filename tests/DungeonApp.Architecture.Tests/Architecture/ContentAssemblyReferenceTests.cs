using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The downward half of "Warstwy i granice": DungeonApp.Core and DungeonApp.Desktop must not
/// reference any content assembly. A zestaw is the only place in the app allowed to know what a
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
