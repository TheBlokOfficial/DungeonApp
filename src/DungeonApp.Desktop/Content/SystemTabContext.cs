using DungeonApp.Core.Content;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// The narrow window a system's own tab in the System sidebar category gets - today only the
/// content registry. Deliberately narrow: docs/architecture.md's "Pasek boczny: trzy kategorie"
/// says a System-category tab does not see the open campaign at all, and this type is what makes
/// that structural rather than a convention a tab factory has to remember - there is nothing here
/// to read a campaign from.
/// <para>
/// Transitional per docs/tasks.md's step 1 brief: once step 4 moves the registry itself into the
/// library, this context may carry more without reaching back into the shell for it.
/// </para>
/// </summary>
public sealed class SystemTabContext(ContentRegistry registry)
{
    public ContentRegistry Registry { get; } = registry;
}
