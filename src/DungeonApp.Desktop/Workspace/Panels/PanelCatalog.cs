using System.Collections.Generic;
using System.Linq;

namespace DungeonApp.Desktop.Workspace.Panels;

/// <summary>
/// Which panels one open campaign's desk offers, and the only place to ask for one by id.
/// <para>
/// Created for one campaign because its desk offers windows belonging to the current session.
/// No panel ids are listed here: they all come from the selected system's tool strip,
/// and the shell currently contributes no panels of its own.
/// </para>
/// </summary>
public sealed class PanelCatalog
{
    private PanelCatalog(IReadOnlyList<WorkspacePanelDescriptor> all) => All = all;

    public IReadOnlyList<WorkspacePanelDescriptor> All { get; }

    /// <summary>
    /// <paramref name="tools"/> is what the chosen system's tool list gave
    /// (<see cref="Systems.IGameSystem.CreateDeskTools"/>, passed on by <see cref="CampaignDesk.CreateAsync"/>)
    /// - the catalog's only source, since the shell itself contributes no panel of its
    /// own. Kept as a dedicated step, rather than handing the list straight through, so that "shell
    /// panels first, then system tools" stays the shape of this code even while the first list is
    /// empty - the property that a system can only ever add to the desk, never displace what the
    /// shell offers, still reads off the layout, not off a comment.
    /// </summary>
    public static PanelCatalog For(IReadOnlyList<WorkspacePanelDescriptor> tools) => new([.. tools]);

    /// <summary>
    /// Returns null for an id this campaign does not offer. Restoring a saved layout skips
    /// such entries so that an old layout cannot prevent the campaign from opening.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
