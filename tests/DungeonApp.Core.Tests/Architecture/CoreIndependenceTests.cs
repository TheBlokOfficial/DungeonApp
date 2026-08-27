using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Core.Tests.Architecture;

/// <summary>
/// Granica, na której stoi cały podział na dwa projekty: logika gry nie może wiedzieć o UI.
/// Referencja do Avalonia w Core oznacza, że reguły przestały być testowalne bez okna.
/// </summary>
public sealed class CoreIndependenceTests
{
    [Fact]
    public void Core_does_not_reference_Avalonia()
    {
        var core = Assembly.Load("DungeonApp.Core");

        var uiReferences = core
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null
                && name.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(uiReferences);
    }
}
