using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// The only place panel identifiers are spelled out, built for one open campaign.
/// <para>
/// Per campaign rather than static, because what the desk can offer follows what the campaign has
/// switched on. This build ships no panels: the descriptor set is always empty, and every campaign
/// opens onto a bare desk until a panel is added back.
/// </para>
/// </summary>
public sealed class PanelCatalog
{
    private PanelCatalog(IReadOnlyList<WorkspacePanelDescriptor> all) => All = all;

    public IReadOnlyList<WorkspacePanelDescriptor> All { get; }

    public static PanelCatalog For(CampaignSession session) => new([]);

    /// <summary>
    /// Returns null for an identifier this campaign does not offer. Callers must treat that as
    /// "skip this entry", never as an error: a saved layout naming a panel the campaign no longer
    /// has - because its module was switched off - still has to load.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
