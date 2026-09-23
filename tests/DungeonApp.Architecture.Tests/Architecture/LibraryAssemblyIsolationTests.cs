using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The sibling of <see cref="ContentAssemblyIsolationTests"/> for the library layer
/// (docs/architecture.md, "Rama, biblioteka, system": "Biblioteki nie znają się nawzajem; składa je
/// system"): a library may reference the frame, but never another library - only a system stands
/// above more than one at once.
/// <para>
/// A library is a <em>group</em> of projects, not one project: <c>DungeonApp.Library.Entries</c> and
/// <c>DungeonApp.Library.Entries.Desktop</c> would be the same library (an Avalonia-free core plus its
/// desktop controls), free to reference each other, while <c>DungeonApp.Library.Entries.Desktop</c>
/// and <c>DungeonApp.Library.Workspace</c> are two different libraries and must not. The group is the
/// third dot-segment of the assembly name - see <see cref="LibraryKey"/> - never hardcoded, the same
/// discovery <see cref="RepositoryRoot.LibraryAssemblyNames"/> already uses.
/// </para>
/// <para>
/// On today's code there is exactly one library project, so this test is trivially green - there is
/// nothing yet for the rule to catch. That is expected, not a gap to close here: the moment a second
/// library exists, this scan is what keeps the two from quietly reaching into each other.
/// </para>
/// </summary>
public sealed class LibraryAssemblyIsolationTests
{
    [Fact]
    public void No_library_references_a_different_library()
    {
        var libraryAssemblyNames = RepositoryRoot.LibraryAssemblyNames;
        Assert.NotEmpty(libraryAssemblyNames);

        var libraryAssemblies = libraryAssemblyNames
            .Select(Assembly.Load)
            .ToArray();

        var offences = (
            from assembly in libraryAssemblies
            from reference in assembly.GetReferencedAssemblies()
            where reference.Name is not null
                && libraryAssemblyNames.Contains(reference.Name, StringComparer.Ordinal)
                && !string.Equals(LibraryKey(reference.Name), LibraryKey(assembly.GetName().Name!), StringComparison.Ordinal)
            select $"{assembly.GetName().Name} references {reference.Name}")
            .ToArray();

        Assert.True(
            offences.Length == 0,
            "A library must never reference a different library: " + string.Join(", ", offences));
    }

    /// <summary>
    /// "DungeonApp.Library.&lt;Name&gt;" and "DungeonApp.Library.&lt;Name&gt;.&lt;anything&gt;" are the
    /// same library - the third dot-segment names it. An assembly name with fewer than three segments
    /// is not a shape this discovery produces, but returns itself rather than throwing, so a future
    /// oddly-named project fails the reference assertion loudly instead of failing this helper first.
    /// </summary>
    private static string LibraryKey(string assemblyName)
    {
        var parts = assemblyName.Split('.');
        return parts.Length > 2 ? parts[2] : assemblyName;
    }
}
