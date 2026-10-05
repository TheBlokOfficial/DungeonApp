using System;
using System.Linq;
using System.Reflection;

namespace DungeonApp.Architecture.Tests;

/// <summary>
/// The boundary behind the split into two projects: game logic must not know about the UI.
/// An Avalonia reference in Core means the rules can no longer be tested without a window.
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
