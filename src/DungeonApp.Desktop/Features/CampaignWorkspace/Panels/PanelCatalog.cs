using System.Collections.Generic;
using System.Linq;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Demo;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// The only place panel identifiers are spelled out. A static list of stand-ins for now: this
/// increment builds the window mechanics, not the campaign.
/// <para>
/// The default placements tile the desk exactly as it measures in the reference 1280x800 window:
/// 1032 x 724 DIP at the Medium profile, once the sidebar, top bar and status bar are subtracted.
/// Starting edges and gaps follow the configured quarter-cell (8 DIP) snap step, with no artificial
/// outer margin. A trailing panel reaches the real surface edge even when the runtime surface extent
/// itself is not a whole snap interval.
/// </para>
/// </summary>
public static class PanelCatalog
{
    public const string ClockId = "session.clock";
    public const string PartyId = "session.party";
    public const string HistoryId = "session.history";

    public static IReadOnlyList<WorkspacePanelDescriptor> All { get; } =
    [
        new WorkspacePanelDescriptor(
            PartyId,
            "Drużyna",
            "DungeonIconUsers",
            WorkspacePanelGroup.Session,
            new PanelPlacement(0, 0, 664, 320),
            // FluidData: takes whatever space it is given.
            new PanelConstraints(320, 160, double.PositiveInfinity, double.PositiveInfinity),
            () => new DemoPanelViewModel(
                "Drużyna",
                "Atrapa panelu. Prawdziwa lista postaci pojawi się razem z domeną kampanii.")),

        new WorkspacePanelDescriptor(
            ClockId,
            "Czas świata",
            "DungeonIconClock",
            WorkspacePanelGroup.Session,
            new PanelPlacement(672, 0, 360, 320),
            // Bounded 240-360, straight out of the UI contract's panel growth strategies.
            new PanelConstraints(240, 152, 360, double.PositiveInfinity),
            () => new DemoPanelViewModel(
                "Czas świata",
                "Atrapa panelu. Zegar świata wróci razem z modułem czasu.")),

        new WorkspacePanelDescriptor(
            HistoryId,
            "Historia zmian",
            "DungeonIconHistory",
            WorkspacePanelGroup.Session,
            new PanelPlacement(0, 328, 1032, 396),
            new PanelConstraints(320, 160, double.PositiveInfinity, double.PositiveInfinity),
            () => new DemoPanelViewModel(
                "Historia zmian",
                "Atrapa panelu. Wyjaśnialna historia zdarzeń to zadanie warstwy domenowej."))
    ];

    /// <summary>
    /// Returns null for an identifier the build no longer knows. Callers must treat that as
    /// "skip this entry", never as an error: a saved layout naming a removed panel has to load.
    /// </summary>
    public static WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
