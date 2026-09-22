using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Content;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Content;

/// <summary>
/// Built once in the composition root: the single place that turns every installed system's
/// tool belt into the panels one open campaign's <see cref="Features.CampaignWorkspace.Panels.PanelCatalog"/>
/// offers.
/// <para>
/// <paramref name="registry"/> is a <see cref="Func{TResult}"/>, not a <see cref="ContentRegistry"/>
/// value, and that is load-bearing: at composition-root time (<c>App.axaml.cs</c>'s
/// <c>Initialize</c>), packs have not been loaded yet, so there is no registry to read. Reading it is
/// deferred to <see cref="ToolsFor"/>, called only once a campaign is actually opened - the shell
/// already defers the registry screen's own dependency the same way (see the
/// <see cref="Func{TResult}"/> of <c>ContentRegistry</c> threaded through <c>AppShellViewModel</c>).
/// </para>
/// </summary>
public sealed class CampaignToolProvider(
    IReadOnlyList<IGameSystem> systems,
    Func<ContentRegistry> registry,
    IContentTypeCatalog types)
{
    public IReadOnlyList<WorkspacePanelDescriptor> ToolsFor(CampaignSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var context = new CampaignToolContext(session, registry(), types);

        return [.. systems.SelectMany(system => system.CreateTools(context))];
    }
}
