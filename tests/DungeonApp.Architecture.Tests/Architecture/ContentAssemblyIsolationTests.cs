using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// Zestawy never reference each other ("Warstwy i granice"). Today there is exactly one
/// (DungeonApp.Content.Dnd5e), so a naive version of this test would pass whether or not the rule
/// actually held, simply because there is nothing yet to violate it - the same trap
/// <see cref="CoreEntryKindIndependenceTests.The_scan_reaches_engine_shell_and_the_content_dictionary"/>
/// guards against for the vocabulary scan. The assertion that at least one zestaw was found closes
/// that gap.
/// <para>
/// The edge this forbids is one <i>within</i> a single layer: two content assemblies depending on
/// each other would bring the diamond problem, a load order that has to be gotten right, and
/// cascading version bumps - the trouble the strictly-downward layering everywhere else in this app
/// exists to avoid. None of that has a place between two things that both claim to be "the one spot
/// that knows about a given game system".
/// </para>
/// <para>
/// Discovering "every zestaw that exists" from <see cref="Assembly.GetReferencedAssemblies"/> would
/// be unreliable: an unused project reference can be trimmed by the compiler and never show up in
/// the emitted metadata, so a passing result could mean either "no cross-references" or "the
/// reference exists but nobody uses it yet". This instead lists every
/// <c>DungeonApp.Content.*.csproj</c> under <c>src/</c> and loads each one by its simple name -
/// deliberately not <see cref="Assembly.LoadFrom(string)"/> against a path built from a
/// configuration-specific <c>bin</c> folder, which breaks the moment Debug becomes Release, and
/// deliberately not a scan of whatever is already sitting in the <see cref="AppDomain"/>, which
/// depends on what some earlier test happened to touch first. Loading by simple name works here
/// because this test project already carries a project reference to every content project it needs
/// to inspect (see the <c>.csproj</c>), so the runtime's own probing resolves it exactly the way
/// <c>Assembly.Load("DungeonApp.Core")</c> already does in <see cref="ContentAssemblyReferenceTests"/>.
/// </para>
/// </summary>
public sealed class ContentAssemblyIsolationTests
{
    [Fact]
    public void No_content_assembly_references_another_content_assembly()
    {
        var contentAssemblyNames = ContentAssemblyNamesUnderSrc();
        Assert.NotEmpty(contentAssemblyNames);

        var contentAssemblies = contentAssemblyNames
            .Select(Assembly.Load)
            .ToArray();

        var offences = (
            from assembly in contentAssemblies
            from reference in assembly.GetReferencedAssemblies()
            where reference.Name is not null
                && contentAssemblyNames.Contains(reference.Name, StringComparer.Ordinal)
                && reference.Name != assembly.GetName().Name
            select $"{assembly.GetName().Name} references {reference.Name}")
            .ToArray();

        Assert.True(
            offences.Length == 0,
            "A zestaw must never reference another zestaw: " + string.Join(", ", offences));
    }

    private static IReadOnlyList<string> ContentAssemblyNamesUnderSrc() =>
    [
        .. Directory
            .EnumerateFiles(RepositoryRoot.SrcSources, "DungeonApp.Content.*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Select(name => name!)
    ];
}
