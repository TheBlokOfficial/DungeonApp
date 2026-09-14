using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// Which panels one open campaign's desk offers, and the only place to ask for one by id.
/// <para>
/// Powstaje dla jednej kampanii, bo blat oferuje okna należące do jej bieżącej sesji. Żaden
/// identyfikator panelu nie jest już tutaj wypisany: wszystkie przychodzą z pasów narzędzi
/// zestawów treści, a powłoka nie wnosi dziś własnego panelu.
/// </para>
/// </summary>
public sealed class PanelCatalog
{
    private PanelCatalog(IReadOnlyList<WorkspacePanelDescriptor> all) => All = all;

    public IReadOnlyList<WorkspacePanelDescriptor> All { get; }

    /// <summary>
    /// <paramref name="tools"/> is what the campaign's installed content sets bring
    /// (<see cref="Content.CampaignToolProvider.ToolsFor"/>) - today the catalog's only source, since
    /// the shell itself contributes no panel of its own. Kept as a dedicated step, rather than handing
    /// the list straight through, so that "shell panels first, then zestaw tools" stays the shape of
    /// this code even while the first list is empty - the property that a zestaw can only ever add to
    /// the desk, never displace what the shell offers, still reads off the layout, not off a comment.
    /// </summary>
    public static PanelCatalog For(IReadOnlyList<WorkspacePanelDescriptor> tools) => new([.. tools]);

    /// <summary>
    /// Zwraca null dla identyfikatora, którego ta kampania nie oferuje. Odtworzenie zapisu pomija
    /// taki wpis, aby stary układ nie blokował otwarcia kampanii.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
