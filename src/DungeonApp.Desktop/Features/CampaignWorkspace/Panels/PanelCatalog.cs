using System;
using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// The only place panel identifiers are spelled out, built for one open campaign.
/// <para>
/// Powstaje dla jednej kampanii, bo blat oferuje okna należące do jej bieżącej sesji.
/// </para>
/// </summary>
public sealed class PanelCatalog
{
    private PanelCatalog(IReadOnlyList<WorkspacePanelDescriptor> all) => All = all;

    public IReadOnlyList<WorkspacePanelDescriptor> All { get; }

    /// <summary>
    /// <paramref name="extraTools"/> is what the campaign's installed content sets bring
    /// (<see cref="Content.CampaignToolProvider.ToolsFor"/>) - appended after the built-in counter, so
    /// a zestaw can only ever add to the desk, never displace what the shell itself always offers.
    /// </summary>
    public static PanelCatalog For(CampaignSession session, IReadOnlyList<WorkspacePanelDescriptor> extraTools)
    {
        var minimum = WorkspaceMetrics.Fallback;
        var minWidth = Math.Max(minimum.MinPanelWidth, WorkspaceGridSettings.CounterPanelMinWidth);
        var minHeight = Math.Max(minimum.MinPanelHeight, WorkspaceGridSettings.CounterPanelMinHeight);

        return new(
        [
            new WorkspacePanelDescriptor(
                "counter",
                "Licznik",
                "DungeonIconDatabase",
                WorkspacePanelGroup.Session,
                new PanelPlacement(
                    WorkspaceGridSettings.CellSize,
                    WorkspaceGridSettings.CellSize,
                    minWidth,
                    minHeight),
                new PanelConstraints(
                    minWidth,
                    minHeight,
                    WorkspaceGridSettings.CounterPanelMaxWidth,
                    WorkspaceGridSettings.CounterPanelMaxHeight),
                () => new CounterPanelViewModel(session)),
            .. extraTools,
        ]);
    }

    /// <summary>
    /// Zwraca null dla identyfikatora, którego ta kampania nie oferuje. Odtworzenie zapisu pomija
    /// taki wpis, aby stary układ nie blokował otwarcia kampanii.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
