using System.Collections.Generic;
using System.Linq;
using DungeonApp.Core.Modules.Clock;
using DungeonApp.Core.Modules.Party;
using DungeonApp.Core.Modules.Scheduler;
using DungeonApp.Desktop.Controls.Workspace;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Clock;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.History;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Party;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels.Scheduler;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>
/// The only place panel identifiers are spelled out, built for one open campaign.
/// <para>
/// Per campaign rather than static, because what the desk can offer follows what the campaign has
/// switched on: a campaign without the clock module has no clock panel to open, and a saved layout
/// naming one is skipped the same way a layout naming a removed panel is.
/// </para>
/// <para>
/// The default placements tile the desk exactly as it measures in the reference 1280x800 window:
/// 1032 x 724 DIP at the Medium profile, once the sidebar, top bar and status bar are subtracted.
/// Starting edges and gaps follow the configured quarter-cell (8 DIP) snap step, with no artificial
/// outer margin. A trailing panel reaches the real surface edge even when the runtime surface extent
/// itself is not a whole snap interval.
/// </para>
/// </summary>
public sealed class PanelCatalog
{
    public const string ClockId = "session.clock";
    public const string SchedulerId = "session.scheduler";
    public const string PartyId = "session.party";
    public const string HistoryId = "session.history";

    private PanelCatalog(IReadOnlyList<WorkspacePanelDescriptor> all) => All = all;

    public IReadOnlyList<WorkspacePanelDescriptor> All { get; }

    public static PanelCatalog For(
        CampaignSession session,
        IReadOnlyList<ChronicleEntryViewModel> initialChronicle)
    {
        var descriptors = new List<WorkspacePanelDescriptor>();

        if (session.Campaign.Modules.TryGet<PartyModule>(out var party))
        {
            descriptors.Add(new WorkspacePanelDescriptor(
                PartyId,
                "Drużyna",
                "DungeonIconUsers",
                WorkspacePanelGroup.Session,
                new PanelPlacement(0, 0, 664, 320),
                // FluidData: takes whatever space it is given.
                new PanelConstraints(320, 160, double.PositiveInfinity, double.PositiveInfinity),
                () => new PartyPanelViewModel(session, party)));
        }

        if (session.Campaign.Modules.TryGet<ClockModule>(out var clock))
        {
            descriptors.Add(new WorkspacePanelDescriptor(
                ClockId,
                "Czas świata",
                "DungeonIconClock",
                WorkspacePanelGroup.Session,
                new PanelPlacement(672, 0, 360, 320),
                // Bounded 240-360, straight out of the UI contract's panel growth strategies.
                new PanelConstraints(240, 200, 360, double.PositiveInfinity),
                () => new ClockPanelViewModel(session, clock)));

            // Only alongside the clock, which the module itself already requires: a plan measured in
            // world time is meaningless without something to measure it against.
            if (session.Campaign.Modules.TryGet<SchedulerModule>(out var scheduler))
            {
                descriptors.Add(new WorkspacePanelDescriptor(
                    SchedulerId,
                    "Harmonogram",
                    "DungeonIconHistory",
                    WorkspacePanelGroup.World,
                    new PanelPlacement(672, 328, 360, 396),
                    new PanelConstraints(280, 240, double.PositiveInfinity, double.PositiveInfinity),
                    () => new SchedulerPanelViewModel(session, scheduler, clock)));
            }
        }

        // Always offered. The chronicle belongs to the campaign itself, not to any module, so
        // there is no configuration under which a campaign has nothing to explain.
        descriptors.Add(new WorkspacePanelDescriptor(
            HistoryId,
            "Kronika",
            "DungeonIconHistory",
            WorkspacePanelGroup.Campaign,
            new PanelPlacement(0, 328, 664, 396),
            new PanelConstraints(320, 200, double.PositiveInfinity, double.PositiveInfinity),
            () => new HistoryPanelViewModel(session, initialChronicle)));

        return new PanelCatalog(descriptors);
    }

    /// <summary>
    /// Returns null for an identifier this campaign does not offer. Callers must treat that as
    /// "skip this entry", never as an error: a saved layout naming a panel the campaign no longer
    /// has - because its module was switched off - still has to load.
    /// </summary>
    public WorkspacePanelDescriptor? Find(string id) =>
        All.FirstOrDefault(descriptor => descriptor.Id == id);
}
