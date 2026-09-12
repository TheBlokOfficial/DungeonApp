using System;

namespace DungeonApp.Desktop.Tests;

public sealed class AppTests
{
    /// <summary>
    /// The guard has to run before <c>AvaloniaXamlLoader.Load</c>, which is what makes this
    /// assertable without standing up Avalonia at all. The message is asserted too, not only the
    /// exception type: the property under test is that an app built without content fails
    /// <i>loudly</i>, and an exception nobody can act on would satisfy the type alone.
    /// </summary>
    [Fact]
    public void Initialize_throws_and_names_the_composition_root_when_built_without_any_content_set()
    {
        var app = new App();

        var error = Assert.Throws<InvalidOperationException>(() => app.Initialize());

        Assert.Contains("zestaw", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DungeonApp.App/Program.cs", error.Message, StringComparison.Ordinal);
    }
}
