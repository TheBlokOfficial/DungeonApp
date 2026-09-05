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

    public static PanelCatalog For(CampaignSession session)
    {
        var minimum = WorkspaceMetrics.Fallback;

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
                    minimum.MinPanelWidth,
                    minimum.MinPanelHeight),
                new PanelConstraints(
                    minimum.MinPanelWidth,
                    minimum.MinPanelHeight,
                    double.PositiveInfinity,
                    double.PositiveInfinity),
                () => new CounterPanelViewModel(session)),
        ]);
    }

    /// <summary>
    /// Zwraca null dla identyfikatora, którego ta kampania nie oferuje. Odtworzenie zapisu pomija
    /// taki wpis, aby stary układ nie blokował otwarcia kampanii.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
